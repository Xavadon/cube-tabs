using System.Collections.Generic;
using _Project.Scripts.Architecture.Services.Input;
using _Project.Scripts.Architecture.Utils;
using _Project.Scripts.Gameplay.Character;
using _Project.Scripts.Gameplay.Character.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Camera
{
    public class CameraService : ICameraService
    {
        private const string ConfigPath = "Data/CameraConfig";
        private const string UserZoomKey = "settings_camera_zoom";
        private const float SmoothTime = 0.5f;
        private const float FreeMoveSpeed = 20f;
        private const float FreeLookSensitivity = 0.15f;
        private const float FreeMinPitch = -89f;
        private const float FreeMaxPitch = 89f;
        private const float ShakeNoiseSeedY = 37.1f;

        private static readonly Vector3 ThirdPersonOffset = new(0f, 3f, -5f);
        private static readonly Vector3 FirstPersonOffset = new(0f, 1.6f, 0f);

        public bool CameraLocked { get; private set; }
        public CameraMode CurrentMode { get; private set; } = CameraMode.TopDown;

        private readonly ICharacterRegistry _characterRegistry;
        private readonly IInputService _inputService;
        private CameraConfig _config;
        private UnityEngine.Camera _playerCamera;
        private Vector3 _smoothPosition;
        private Vector3 _positionVelocity;
        private Transform _target;

        private float _freeYaw;
        private float _freePitch;

        private bool _framingInitialized;
        private Vector3 _smoothCenter;
        private Vector3 _centerVelocity;
        private float _smoothDistance;
        private float _distanceVelocity;
        private bool _yawInitialized;
        private float _targetYaw;
        private float _smoothYaw;
        private float _yawVelocity;

        private float _trauma;
        private Vector3 _appliedShakeOffset;

        private int _screenHeight;
        private float _distanceScale = 1f;

        private float _userZoom = 1f;
        private float _smoothUserZoom = 1f;
        private float _userZoomVelocity;
        private bool _userZoomDirty;
        private Vector3 _panOffset;
        private Vector3 _panVelocity;
        private float _lastGestureTime = float.NegativeInfinity;

        public CameraService(ICharacterRegistry characterRegistry, IInputService inputService)
        {
            _characterRegistry = characterRegistry;
            _inputService = inputService;
        }

        public UniTask Initialize()
        {
            _config = Resources.Load<CameraConfig>(ConfigPath);
            if (_config == null)
            {
                Debug.LogError($"[CameraService] CameraConfig not found at '{ConfigPath}', using defaults");
                _config = ScriptableObject.CreateInstance<CameraConfig>();
            }

            _userZoom = Mathf.Clamp(PlayerPrefs.GetFloat(UserZoomKey, 1f), _config.MinUserZoom, _config.MaxUserZoom);
            _smoothUserZoom = _userZoom;

            _playerCamera = UnityEngine.Camera.main;
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            if (_userZoomDirty)
            {
                PlayerPrefs.SetFloat(UserZoomKey, _userZoom);
                PlayerPrefs.Save();
                _userZoomDirty = false;
            }

            _playerCamera = null;
            _target = null;
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void CycleMode()
        {
            CameraMode nextMode = CurrentMode switch
            {
                CameraMode.TopDown => CameraMode.Free,
                CameraMode.Free => CameraMode.ThirdPerson,
                CameraMode.ThirdPerson => CameraMode.FirstPerson,
                CameraMode.FirstPerson => CameraMode.TopDown,
                _ => CameraMode.TopDown
            };

            if (nextMode == CameraMode.Free)
                InitializeFreeMode();

            if (nextMode == CameraMode.TopDown)
            {
                _framingInitialized = false;
            }

            CurrentMode = nextMode;
        }

        public void ResetToDefault()
        {
            CurrentMode = CameraMode.TopDown;
            ResetFraming();
        }

        public void AddTrauma(float amount, Vector3 sourcePosition)
        {
            if (amount <= 0f)
            {
                return;
            }

            Vector3 reference = _smoothCenter;
            if (CurrentMode != CameraMode.TopDown && _playerCamera != null)
            {
                reference = _playerCamera.transform.position;
            }

            float falloff = 1f - Mathf.Clamp01(Vector3.Distance(sourcePosition, reference) / _config.ShakeFalloffDistance);
            _trauma = Mathf.Clamp01(_trauma + amount * falloff);
        }

        public void Pan(Vector2 screenDelta)
        {
            if (CurrentMode != CameraMode.TopDown || _playerCamera == null)
            {
                return;
            }

            float halfFov = _playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float visibleHeight = 2f * _smoothDistance * _smoothUserZoom * Mathf.Tan(halfFov);
            float pitchSin = Mathf.Sin(_config.Pitch * Mathf.Deg2Rad);

            Quaternion yaw = Quaternion.Euler(0f, _smoothYaw, 0f);
            Vector3 right = yaw * Vector3.right;
            Vector3 forward = yaw * Vector3.forward;

            Vector3 offset = _panOffset
                             - right * (screenDelta.x * visibleHeight)
                             + forward * (screenDelta.y * visibleHeight / pitchSin);
            _panOffset = Vector3.ClampMagnitude(offset, _config.MaxPanOffset);
            _panVelocity = Vector3.zero;
            _lastGestureTime = Time.unscaledTime;
        }

        public void Zoom(float factor)
        {
            if (CurrentMode != CameraMode.TopDown || factor <= 0f)
            {
                return;
            }

            _userZoom = Mathf.Clamp(_userZoom * factor, _config.MinUserZoom, _config.MaxUserZoom);
            _userZoomDirty = true;
            _lastGestureTime = Time.unscaledTime;
        }

        public void ResetUserView()
        {
            _userZoom = 1f;
            _userZoomDirty = true;
            _lastGestureTime = float.NegativeInfinity;
        }

        private void InitializeFreeMode()
        {
            if (_playerCamera == null) return;

            Vector3 euler = _playerCamera.transform.eulerAngles;
            _freeYaw = euler.y;
            _freePitch = euler.x > 180f ? euler.x - 360f : euler.x;
        }

        public void Tick(float deltaTime)
        {
            if (_playerCamera == null)
            {
                _playerCamera = UnityEngine.Camera.main;
                if (_playerCamera == null)
                    return;

                _appliedShakeOffset = Vector3.zero;
                ResetFraming();
            }

            float unscaledDeltaTime = Time.unscaledDeltaTime;

            RemoveShake();
            UpdateDistanceScale();

            if (CurrentMode != CameraMode.TopDown && CurrentMode != CameraMode.Free && _target == null)
                CurrentMode = CameraMode.TopDown;

            switch (CurrentMode)
            {
                case CameraMode.TopDown:
                    TickTopDown(unscaledDeltaTime);
                    break;
                case CameraMode.Free:
                    TickFree(deltaTime);
                    break;
                case CameraMode.ThirdPerson:
                    TickThirdPerson();
                    break;
                case CameraMode.FirstPerson:
                    TickFirstPerson();
                    break;
            }

            ApplyShake(unscaledDeltaTime);
        }

        private void UpdateDistanceScale()
        {
            if (Screen.height == _screenHeight)
            {
                return;
            }

            _screenHeight = Screen.height;
            _distanceScale = 1f;
            if (ScreenDensity.GetScreenHeightDp() < _config.CompactScreenHeightDp)
            {
                _distanceScale = _config.CompactDistanceScale;
            }
        }

        private void ResetFraming()
        {
            _framingInitialized = false;
            _yawInitialized = false;
            _trauma = 0f;
            _panOffset = Vector3.zero;
            _panVelocity = Vector3.zero;
        }

        private void TickFree(float deltaTime)
        {
            Transform camTransform = _playerCamera.transform;

            if (_inputService.IsRightMousePressed)
            {
                Vector2 lookInput = _inputService.LookInput;
                _freeYaw += lookInput.x * FreeLookSensitivity;
                _freePitch -= lookInput.y * FreeLookSensitivity;
                _freePitch = Mathf.Clamp(_freePitch, FreeMinPitch, FreeMaxPitch);
            }

            camTransform.rotation = Quaternion.Euler(_freePitch, _freeYaw, 0f);

            Vector2 moveInput = _inputService.MoveInput;
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 forward = camTransform.forward;
                Vector3 right = camTransform.right;

                Vector3 movement = (forward * moveInput.y + right * moveInput.x) * (FreeMoveSpeed * deltaTime);
                camTransform.position += movement;
            }
        }

        private void TickTopDown(float deltaTime)
        {
            if (!TryGetFocusBounds(out Bounds focus))
            {
                return;
            }

            float radius = Mathf.Max(focus.extents.x, focus.extents.z) + _config.FramePadding;
            float halfFov = _playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float desiredDistance = Mathf.Clamp(radius / Mathf.Tan(halfFov), _config.MinDistance, _config.MaxDistance) * _distanceScale;

            if (!_framingInitialized)
            {
                _smoothCenter = focus.center;
                _smoothDistance = desiredDistance;
                _smoothYaw = _targetYaw;
                _centerVelocity = Vector3.zero;
                _distanceVelocity = 0f;
                _yawVelocity = 0f;
                _framingInitialized = true;
            }

            float zoomSmoothTime;
            if (desiredDistance > _smoothDistance)
            {
                zoomSmoothTime = _config.ZoomOutSmoothTime;
            }
            else
            {
                zoomSmoothTime = _config.ZoomInSmoothTime;
            }

            _smoothCenter = Vector3.SmoothDamp(_smoothCenter, focus.center, ref _centerVelocity, _config.CenterSmoothTime, Mathf.Infinity, deltaTime);
            _smoothDistance = Mathf.SmoothDamp(_smoothDistance, desiredDistance, ref _distanceVelocity, zoomSmoothTime, Mathf.Infinity, deltaTime);
            _smoothYaw = Mathf.SmoothDampAngle(_smoothYaw, _targetYaw, ref _yawVelocity, _config.YawSmoothTime, Mathf.Infinity, deltaTime);
            _smoothUserZoom = Mathf.SmoothDamp(_smoothUserZoom, _userZoom, ref _userZoomVelocity, _config.UserZoomSmoothTime, Mathf.Infinity, deltaTime);
            ReturnPan(deltaTime);

            Quaternion rotation = Quaternion.Euler(_config.Pitch, _smoothYaw, 0f);
            Vector3 lookPoint = _smoothCenter + _panOffset + Vector3.up * _config.LookHeight;
            float distance = _smoothDistance * _smoothUserZoom;
            _playerCamera.transform.SetPositionAndRotation(lookPoint - rotation * Vector3.forward * distance, rotation);
        }

        private void ReturnPan(float deltaTime)
        {
            if (Time.unscaledTime - _lastGestureTime < _config.PanReturnDelay)
            {
                return;
            }

            _panOffset = Vector3.SmoothDamp(_panOffset, Vector3.zero, ref _panVelocity, _config.PanReturnSmoothTime, Mathf.Infinity, deltaTime);
        }

        private bool TryGetFocusBounds(out Bounds focus)
        {
            IReadOnlyList<Character> allies = _characterRegistry.GetAllies();
            IReadOnlyList<Character> enemies = _characterRegistry.GetEnemies();

            if (!_yawInitialized)
            {
                _targetYaw = _playerCamera.transform.eulerAngles.y;
                _yawInitialized = true;
            }

            if (!TryGetClusterBounds(allies, out focus))
            {
                return TryGetClusterBounds(enemies, out focus);
            }

            Vector3 allyCenter = focus.center;
            float allyRadius = Mathf.Max(focus.extents.x, focus.extents.z);
            float engageRadius = allyRadius + _config.EngageDistance;
            float engageRadiusSqr = engageRadius * engageRadius;

            bool hasEngaged = false;
            bool hasNearest = false;
            float nearestSqr = float.MaxValue;
            Vector3 nearestPosition = Vector3.zero;
            Vector3 enemySum = Vector3.zero;
            int enemyCount = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                Character enemy = enemies[i];
                if (enemy == null)
                {
                    continue;
                }

                Vector3 position = enemy.transform.position;
                enemySum += position;
                enemyCount++;

                float sqr = FlatSqrDistance(position, allyCenter);
                if (sqr <= engageRadiusSqr)
                {
                    focus.Encapsulate(position);
                    hasEngaged = true;
                }

                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearestPosition = position;
                    hasNearest = true;
                }
            }

            if (enemyCount > 0)
            {
                UpdateTargetYaw(allyCenter, enemySum / enemyCount);
            }

            if (!hasEngaged && hasNearest)
            {
                Vector3 direction = nearestPosition - allyCenter;
                direction.y = 0f;
                float leadLength = Mathf.Min(Mathf.Sqrt(nearestSqr), allyRadius + _config.LeadDistance);
                focus.Encapsulate(allyCenter + direction.normalized * leadLength);
            }

            return true;
        }

        private bool TryGetClusterBounds(IReadOnlyList<Character> characters, out Bounds bounds)
        {
            bounds = default;

            Vector3 sum = Vector3.zero;
            int count = 0;

            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] == null)
                {
                    continue;
                }

                sum += characters[i].transform.position;
                count++;
            }

            if (count == 0)
            {
                return false;
            }

            Vector3 centroid = sum / count;
            float clusterRadiusSqr = _config.ClusterRadius * _config.ClusterRadius;
            bool initialized = false;

            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] == null)
                {
                    continue;
                }

                Vector3 position = characters[i].transform.position;
                if (FlatSqrDistance(position, centroid) > clusterRadiusSqr)
                {
                    continue;
                }

                if (!initialized)
                {
                    bounds = new Bounds(position, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(position);
                }
            }

            if (!initialized)
            {
                bounds = new Bounds(centroid, Vector3.zero);
            }

            return true;
        }

        private void UpdateTargetYaw(Vector3 allyCenter, Vector3 enemyCenter)
        {
            Vector3 direction = enemyCenter - allyCenter;
            direction.y = 0f;

            if (direction.sqrMagnitude < _config.MinYawSeparation * _config.MinYawSeparation)
            {
                return;
            }

            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(_targetYaw, yaw)) > _config.YawDeadZone)
            {
                _targetYaw = yaw;
            }
        }

        private static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private void RemoveShake()
        {
            _playerCamera.transform.position -= _appliedShakeOffset;
            _appliedShakeOffset = Vector3.zero;
        }

        private void ApplyShake(float deltaTime)
        {
            if (_trauma <= 0f)
            {
                return;
            }

            _trauma = Mathf.Max(0f, _trauma - _config.TraumaDecay * deltaTime);

            float strength = _trauma * _trauma * _config.MaxShakeOffset * _distanceScale * _smoothUserZoom;
            float time = Time.unscaledTime * _config.ShakeFrequency;
            float offsetX = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f * strength;
            float offsetY = (Mathf.PerlinNoise(0f, time + ShakeNoiseSeedY) - 0.5f) * 2f * strength;

            Transform camTransform = _playerCamera.transform;
            _appliedShakeOffset = camTransform.right * offsetX + camTransform.up * offsetY;
            camTransform.position += _appliedShakeOffset;
        }

        private void TickThirdPerson()
        {
            Vector3 targetPos = _target.position;
            Vector3 desiredPosition = targetPos
                                      + _target.right * ThirdPersonOffset.x
                                      + Vector3.up * ThirdPersonOffset.y
                                      + _target.forward * ThirdPersonOffset.z;

            Transform camTransform = _playerCamera.transform;
            _smoothPosition = Vector3.SmoothDamp(_smoothPosition, desiredPosition, ref _positionVelocity, SmoothTime);
            camTransform.position = _smoothPosition;
            camTransform.LookAt(targetPos + Vector3.up * FirstPersonOffset.y);
        }

        private void TickFirstPerson()
        {
            Transform camTransform = _playerCamera.transform;
            camTransform.position = _target.TransformPoint(FirstPersonOffset);
            camTransform.rotation = _target.rotation;
        }
    }
}
