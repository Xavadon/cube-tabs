using System.Collections.Generic;
using _Project.Scripts.Architecture.Services.Input;
using _Project.Scripts.Gameplay.Character;
using _Project.Scripts.Gameplay.Character.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Scripts.Architecture.Services.Camera
{
    public class CameraService : ICameraService
    {
        private const float SmoothTime = 0.5f;
        private const float MinDistance = 20f;
        private const float MaxDistance = 50f;
        private const float BoundsPadding = 3f;
        private const float FreeMoveSpeed = 20f;
        private const float FreeLookSensitivity = 0.15f;
        private const float FreeMinPitch = -89f;
        private const float FreeMaxPitch = 89f;

        private static readonly Vector3 BaseOffset = new(0f, 7f, -10f);
        private static readonly Vector3 TopDownOffset = new(0f, 16f, -11f);
        private static readonly Vector3 ThirdPersonOffset = new(0f, 3f, -5f);
        private static readonly Vector3 FirstPersonOffset = new(0f, 1.6f, 0f);

        public bool CameraLocked { get; private set; }
        public CameraMode CurrentMode { get; private set; } = CameraMode.TopDown;

        private readonly ICharacterRegistry _characterRegistry;
        private readonly IInputService _inputService;
        private IReadOnlyList<Character> _characters;
        private UnityEngine.Camera _playerCamera;
        private Vector3 _smoothCenter;
        private Vector3 _centerVelocity;
        private Vector3 _smoothPosition;
        private Vector3 _positionVelocity;
        private Transform _target;

        private float _freeYaw;
        private float _freePitch;

        public CameraService(ICharacterRegistry characterRegistry, IInputService inputService)
        {
            _characterRegistry = characterRegistry;
            _inputService = inputService;
        }

        public UniTask Initialize()
        {
            _playerCamera = UnityEngine.Camera.main;
            _characters = _characterRegistry.GetAll();
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _characters = null;
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

            CurrentMode = nextMode;
        }

        public void ResetToDefault()
        {
            CurrentMode = CameraMode.TopDown;
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
            }

            if (CurrentMode != CameraMode.TopDown && CurrentMode != CameraMode.Free && _target == null)
                CurrentMode = CameraMode.TopDown;

            switch (CurrentMode)
            {
                case CameraMode.TopDown:
                    TickTopDown();
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

        private void TickTopDown()
        {
            if (_target == null) return;

            Vector3 targetPos = _target.position;
            Vector3 desiredPosition = targetPos + TopDownOffset;

            _smoothPosition = Vector3.SmoothDamp(_smoothPosition, desiredPosition, ref _positionVelocity, SmoothTime);

            Transform camTransform = _playerCamera.transform;
            camTransform.position = _smoothPosition;
            camTransform.LookAt(targetPos);
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
