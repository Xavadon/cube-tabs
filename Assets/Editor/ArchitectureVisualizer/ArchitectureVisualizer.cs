using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Group = UnityEditor.Experimental.GraphView.Group;

namespace Game.Scripts.Editor.ArchitectureVisualizer
{
    public class ArchitectureVisualizerWindow : EditorWindow
    {
        private const string DefaultScriptsPath = "Assets/Game/Scripts";
        private const string WindowTitle = "Architecture Visualizer";
        private const int MinWindowWidth = 1100;
        private const int MinWindowHeight = 700;
        private const int DashboardWidth = 200;

        private ArchitectureGraphView _graphView;
        private string _scriptsPath = DefaultScriptsPath;
        private TextField _pathField;
        private bool _showInterfaces = true;
        private bool _showMonoBehaviours = true;
        private bool _showPlainClasses = true;
        private bool _analyzeDI = false;
        private Label _statsLabel;

        [MenuItem("Tools/Architecture Visualizer")]
        public static void OpenWindow()
        {
            var window = GetWindow<ArchitectureVisualizerWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
        }

        private void OnEnable() => BuildUI();
        private void OnDisable() => rootVisualElement.Clear();

        private void BuildUI()
        {
            rootVisualElement.Clear();

            var mainContainer = CreateMainContainer();
            rootVisualElement.Add(mainContainer);

            var toolbar = CreateToolbar();
            mainContainer.Add(toolbar);

            var contentContainer = CreateContentContainer();
            mainContainer.Add(contentContainer);

            var dashboard = CreateDashboard();
            contentContainer.Add(dashboard);

            var graphContainer = CreateGraphContainer();
            contentContainer.Add(graphContainer);

            _graphView = new ArchitectureGraphView(this);
            _graphView.style.flexGrow = 1;
            graphContainer.Add(_graphView);
        }

        private VisualElement CreateMainContainer()
        {
            var container = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Column
                }
            };
            return container;
        }

        private VisualElement CreateContentContainer()
        {
            var container = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row
                }
            };
            return container;
        }

        private VisualElement CreateGraphContainer()
        {
            var container = new VisualElement { style = { flexGrow = 1 } };
            return container;
        }

        private VisualElement CreateToolbar()
        {
            var toolbar = new VisualElement();
            StyleToolbar(toolbar);

            AddPathControls(toolbar);
            AddActionButtons(toolbar);
            AddSpacer(toolbar, 20);
            AddFilterToggles(toolbar);

            return toolbar;
        }

        private void StyleToolbar(VisualElement toolbar)
        {
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.flexWrap = Wrap.Wrap;
            toolbar.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            toolbar.style.paddingLeft = 8;
            toolbar.style.paddingRight = 8;
            toolbar.style.paddingTop = 6;
            toolbar.style.paddingBottom = 6;
            toolbar.style.minHeight = 36;
        }

        private void AddPathControls(VisualElement parent)
        {
            var pathLabel = new Label("Path:")
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    marginRight = 5,
                    color = Color.white
                }
            };
            parent.Add(pathLabel);

            _pathField = new TextField
            {
                value = _scriptsPath,
                style = { width = 200 }
            };
            _pathField.RegisterValueChangedCallback(evt => _scriptsPath = evt.newValue);
            parent.Add(_pathField);

            var browseButton = new Button(BrowseFolder)
            {
                text = "...",
                style = { width = 25, marginLeft = 3 }
            };
            parent.Add(browseButton);
        }

        private void AddActionButtons(VisualElement parent)
        {
            var analyzeButton = new Button(AnalyzeArchitecture)
            {
                text = "Analyze",
                style =
                {
                    marginLeft = 12,
                    backgroundColor = new Color(0.2f, 0.55f, 0.2f),
                    color = Color.white
                }
            };
            parent.Add(analyzeButton);

            var clearButton = new Button(() => _graphView?.ClearGraph())
            {
                text = "Clear",
                style = { marginLeft = 5 }
            };
            parent.Add(clearButton);
        }

        private void AddFilterToggles(VisualElement parent)
        {
            var interfaceToggle = CreateToggle("Interfaces", _showInterfaces, evt => _showInterfaces = evt.newValue);
            parent.Add(interfaceToggle);

            var monoToggle = CreateToggle("MonoBehaviours", _showMonoBehaviours, evt => _showMonoBehaviours = evt.newValue);
            parent.Add(monoToggle);

            var classToggle = CreateToggle("Classes", _showPlainClasses, evt => _showPlainClasses = evt.newValue);
            parent.Add(classToggle);

            AddSpacer(parent, 20);

            var diToggle = CreateToggle("Analyze DI", _analyzeDI, evt => _analyzeDI = evt.newValue);
            parent.Add(diToggle);
        }

        private Toggle CreateToggle(string label, bool initialValue, EventCallback<ChangeEvent<bool>> callback)
        {
            var toggle = new Toggle(label)
            {
                value = initialValue,
                style = { marginRight = 8 }
            };
            toggle.RegisterValueChangedCallback(callback);
            return toggle;
        }

        private void AddSpacer(VisualElement parent, int width)
        {
            parent.Add(new VisualElement { style = { width = width } });
        }

        private VisualElement CreateDashboard()
        {
            var panel = new VisualElement();
            StyleDashboard(panel);

            AddDashboardTitle(panel);
            AddStatsSection(panel);
            AddSeparator(panel);
            AddQuickFocusSection(panel);
            AddSeparator(panel);
            AddLegendSection(panel);

            return panel;
        }

        private void StyleDashboard(VisualElement panel)
        {
            panel.style.width = DashboardWidth;
            panel.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            panel.style.borderRightWidth = 2;
            panel.style.borderRightColor = new Color(0.1f, 0.1f, 0.1f);
            panel.style.paddingLeft = 10;
            panel.style.paddingRight = 10;
            panel.style.paddingTop = 10;
        }

        private void AddDashboardTitle(VisualElement parent)
        {
            var titleLabel = new Label("[Dashboard]")
            {
                style =
                {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Color.white,
                    marginBottom = 10
                }
            };
            parent.Add(titleLabel);
        }

        private void AddStatsSection(VisualElement parent)
        {
            _statsLabel = new Label("Click Analyze to scan...")
            {
                style =
                {
                    fontSize = 11,
                    color = new Color(0.8f, 0.8f, 0.8f),
                    whiteSpace = WhiteSpace.Normal
                }
            };
            parent.Add(_statsLabel);
        }

        private void AddQuickFocusSection(VisualElement parent)
        {
            var focusTitle = new Label("[Quick Focus]")
            {
                style =
                {
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Color.white,
                    marginBottom = 8
                }
            };
            parent.Add(focusTitle);

            parent.Add(CreateActionButton("> Entry Points", () => _graphView?.FocusEntryPoints()));
            parent.Add(CreateActionButton("> Most Connected", () => _graphView?.FocusMostConnected()));
            parent.Add(CreateActionButton("> Problems", () => _graphView?.FocusProblems()));
            parent.Add(CreateActionButton("> Reset View", () => _graphView?.ResetHighlighting(), 8));
        }

        private Button CreateActionButton(string text, Action callback, int topMargin = 4)
        {
            return new Button(callback)
            {
                text = text,
                style = { marginBottom = 4, marginTop = topMargin }
            };
        }

        private void AddLegendSection(VisualElement parent)
        {
            var legendTitle = new Label("[Legend]")
            {
                style =
                {
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Color.white,
                    marginBottom = 8
                }
            };
            parent.Add(legendTitle);

            AddLegendItem(parent, "[I]", "Interface", new Color(0.3f, 0.7f, 0.3f));
            AddLegendItem(parent, "[M]", "MonoBehaviour", new Color(0.3f, 0.5f, 0.8f));
            AddLegendItem(parent, "[SO]", "ScriptableObject", new Color(0.8f, 0.5f, 0.2f));
            AddLegendItem(parent, "[S]", "Static Class", new Color(0.6f, 0.3f, 0.6f));
            AddLegendItem(parent, "[C]", "Class", new Color(0.5f, 0.5f, 0.5f));

            parent.Add(new VisualElement { style = { height = 8 } });

            AddLegendItem(parent, "[!]", "Circular Dep", new Color(0.9f, 0.3f, 0.3f));
            AddLegendItem(parent, "[G]", "God Class", new Color(0.9f, 0.7f, 0.2f));
            AddLegendItem(parent, "[O]", "Orphan", new Color(0.8f, 0.5f, 0.2f));
            AddLegendItem(parent, "[H]", "High Coupling", new Color(0.3f, 0.5f, 0.8f));
        }

        private void AddSeparator(VisualElement parent)
        {
            var separator = new VisualElement
            {
                style =
                {
                    height = 1,
                    backgroundColor = new Color(0.3f, 0.3f, 0.3f),
                    marginTop = 10,
                    marginBottom = 10
                }
            };
            parent.Add(separator);
        }

        private void AddLegendItem(VisualElement parent, string icon, string text, Color color)
        {
            var container = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2 } };

            var iconLabel = new Label(icon)
            {
                style = { fontSize = 10, color = color, width = 30 }
            };
            container.Add(iconLabel);

            var textLabel = new Label(text)
            {
                style = { fontSize = 10, color = new Color(0.7f, 0.7f, 0.7f) }
            };
            container.Add(textLabel);

            parent.Add(container);
        }

        public void UpdateStats(AnalysisStats stats)
        {
            if (stats == null || _statsLabel == null) return;

            _statsLabel.text = FormatStatsText(stats);
        }

        private string FormatStatsText(AnalysisStats stats)
        {
            return $"Classes: {stats.ClassCount}\n" +
                   $"Interfaces: {stats.InterfaceCount}\n" +
                   $"MonoBehaviours: {stats.MonoBehaviourCount}\n" +
                   $"ScriptableObjects: {stats.ScriptableObjectCount}\n" +
                   $"Static Classes: {stats.StaticClassCount}\n" +
                   $"C# Files: {stats.TotalFiles}\n" +
                   $"-------------\n" +
                   $"Entry Points: {stats.EntryPointCount}\n" +
                   $"Avg Deps: {stats.AvgDependencies:F1}\n" +
                   $"-------------\n" +
                   $"Problems:\n" +
                   $"  [!] Circular: {stats.CircularCount}\n" +
                   $"  [G] God Class: {stats.GodClassCount}\n" +
                   $"  [O] Orphans: {stats.OrphanCount}\n" +
                   $"  [H] High Coupling: {stats.HighCouplingCount}";
        }

        private void BrowseFolder()
        {
            var path = EditorUtility.OpenFolderPanel("Select Scripts Folder", "Assets", "");
            if (string.IsNullOrEmpty(path)) return;

            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }

            _pathField.value = path;
            _scriptsPath = path;
        }

        private void AnalyzeArchitecture()
        {
            if (!ValidateScriptsPath()) return;

            var analyzer = new CodeAnalyzer();
            var types = analyzer.AnalyzeDirectory(_scriptsPath, _analyzeDI);
            var filteredTypes = FilterTypesBySettings(types);

            if (filteredTypes.Count == 0)
            {
                EditorUtility.DisplayDialog("Info", "No types found matching current filters.", "OK");
                return;
            }

            _graphView?.BuildGraph(filteredTypes, analyzer.TotalFilesScanned, analyzer.DIBindings);
        }

        private bool ValidateScriptsPath()
        {
            if (Directory.Exists(_scriptsPath)) return true;

            EditorUtility.DisplayDialog("Error", $"Directory not found: {_scriptsPath}", "OK");
            return false;
        }

        private List<TypeInfo> FilterTypesBySettings(List<TypeInfo> types)
        {
            return types.Where(t =>
                (_showInterfaces && t.Kind == TypeKind.Interface) ||
                (_showMonoBehaviours && t.Kind == TypeKind.MonoBehaviour) ||
                (_showPlainClasses && (t.Kind == TypeKind.Class || t.Kind == TypeKind.StaticClass || t.Kind == TypeKind.ScriptableObject))
            ).ToList();
        }
    }

    public class AnalysisStats
    {
        public int ClassCount { get; set; }
        public int InterfaceCount { get; set; }
        public int MonoBehaviourCount { get; set; }
        public int ScriptableObjectCount { get; set; }
        public int StaticClassCount { get; set; }
        public int TotalFiles { get; set; }
        public int EntryPointCount { get; set; }
        public float AvgDependencies { get; set; }
        public int CircularCount { get; set; }
        public int GodClassCount { get; set; }
        public int OrphanCount { get; set; }
        public int HighCouplingCount { get; set; }
    }

    public class ArchitectureGraphView : GraphView
    {
        private const float GroupPadding = 35f;
        private const float NodeHorizontalGap = 25f;
        private const float NodeVerticalGap = 35f;
        private const float GroupSpacing = 60f;
        private const float StartX = 50f;
        private const float StartY = 50f;
        private const int MaxNodesPerRow = 4;
        private const int GodClassLineThreshold = 500;
        private const int GodClassFieldThreshold = 20;
        private const int HighCouplingThreshold = 10;
        private const float MinGroupWidth = 180f;
        private const float MinGroupHeight = 80f;

        private static readonly Color[] DepthColors = {
            new Color(0.9f, 0.3f, 0.3f),
            new Color(0.9f, 0.6f, 0.2f),
            new Color(0.8f, 0.8f, 0.2f),
            new Color(0.3f, 0.8f, 0.3f),
            new Color(0.3f, 0.6f, 0.9f),
            new Color(0.5f, 0.4f, 0.9f),
            new Color(0.8f, 0.4f, 0.8f),
        };

        private readonly ArchitectureVisualizerWindow _window;
        private readonly Dictionary<string, TypeNode> _nodeMap = new Dictionary<string, TypeNode>();
        private readonly Dictionary<string, Group> _groupMap = new Dictionary<string, Group>();
        private readonly List<Edge> _allEdges = new List<Edge>();
        private readonly List<TypeNode> _entryPoints = new List<TypeNode>();
        private readonly List<TypeNode> _problemNodes = new List<TypeNode>();
        private readonly HashSet<string> _circularPairs = new HashSet<string>();
        private List<DIBinding> _diBindings = new List<DIBinding>();
        private TypeNode _selectedNode;

        public ArchitectureGraphView(ArchitectureVisualizerWindow window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));

            InitializeManipulators();
            InitializeGrid();
            RegisterCallbacks();
        }

        private void InitializeManipulators()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, 2f);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        }

        private void InitializeGrid()
        {
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            style.flexGrow = 1;
        }

        private void RegisterCallbacks()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.target == this)
            {
                ResetHighlighting();
                _selectedNode = null;
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return new List<Port>();
        }

        public void ClearGraph()
        {
            _nodeMap.Clear();
            _groupMap.Clear();
            _allEdges.Clear();
            _entryPoints.Clear();
            _problemNodes.Clear();
            _circularPairs.Clear();
            _selectedNode = null;
            graphElements.ForEach(RemoveElement);
        }

        public void BuildGraph(List<TypeInfo> types, int totalFiles, List<DIBinding> diBindings = null)
        {
            if (types == null || types.Count == 0) return;

            ClearGraph();

            _diBindings = diBindings ?? new List<DIBinding>();

            DetectCircularDependencies(types);
            DetectPatternsAndProblems(types);
            CalculateUsedByRelationships(types);

            CreateFolderGroups(types);
            CreateNodes(types);
            CreateEdges(types);

            UpdateWindowStats(types, totalFiles);
            AutoLayout();
        }

        public void FocusEntryPoints()
        {
            if (_entryPoints.Count == 0) return;

            FocusSpecificNodes(_entryPoints);
        }

        public void FocusMostConnected()
        {
            var topNodes = nodes.ToList().OfType<TypeNode>()
                .OrderByDescending(n => n.TypeInfo.Dependencies.Count + n.TypeInfo.UsedByNames.Count)
                .Take(5)
                .ToList();

            FocusSpecificNodes(topNodes);
        }

        public void FocusProblems()
        {
            foreach (var node in nodes.ToList().OfType<TypeNode>())
            {
                node.style.opacity = _problemNodes.Contains(node) ? 1f : 0.15f;
            }

            foreach (var group in _groupMap.Values)
            {
                group.style.opacity = 0.3f;
            }

            ShowCircularDependencyEdges();
        }

        public void ResetHighlighting()
        {
            foreach (var node in nodes.ToList().OfType<TypeNode>())
            {
                node.style.opacity = 1f;
            }

            foreach (var edge in _allEdges)
            {
                edge.style.display = DisplayStyle.None;
            }

            foreach (var group in _groupMap.Values)
            {
                group.style.opacity = 1f;
            }

            _selectedNode = null;
        }

        private void DetectCircularDependencies(List<TypeInfo> types)
        {
            var typesByName = types.GroupBy(t => t.Name).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var type in types)
            {
                foreach (var dependency in type.Dependencies)
                {
                    if (!typesByName.TryGetValue(dependency, out var dependencyTypes)) continue;

                    foreach (var dependencyType in dependencyTypes)
                    {
                        if (dependencyType.Dependencies.Contains(type.Name))
                        {
                            RegisterCircularDependency(type, dependencyType, dependency);
                        }
                    }
                }
            }
        }

        private void RegisterCircularDependency(TypeInfo type, TypeInfo dependencyType, string dependencyName)
        {
            _circularPairs.Add(CreatePairKey(type.Name, dependencyName));

            if (!type.Problems.Contains(ProblemType.CircularDependency))
            {
                type.Problems.Add(ProblemType.CircularDependency);
            }

            if (!dependencyType.Problems.Contains(ProblemType.CircularDependency))
            {
                dependencyType.Problems.Add(ProblemType.CircularDependency);
            }
        }

        private void DetectPatternsAndProblems(List<TypeInfo> types)
        {
            foreach (var type in types)
            {
                DetectDesignPatterns(type);
                DetectCodeSmells(type, types);
            }
        }

        private void DetectDesignPatterns(TypeInfo type)
        {
            if (type.HasPrivateConstructor && type.HasStaticInstance)
            {
                type.DetectedPatterns.Add("Singleton");
            }

            if (type.Name.Contains("Factory") || type.HasCreateMethod)
            {
                type.DetectedPatterns.Add("Factory");
            }

            if (IsStatePattern(type))
            {
                type.DetectedPatterns.Add("State");
            }

            if (type.HasEventFields || type.Name.Contains("Observer"))
            {
                type.DetectedPatterns.Add("Observer");
            }

            if (type.Name.EndsWith("Service") || type.Name.EndsWith("Manager"))
            {
                type.DetectedPatterns.Add("Service");
            }

            if (IsEntryPoint(type))
            {
                type.IsEntryPoint = true;
            }
        }

        private bool IsStatePattern(TypeInfo type)
        {
            return type.Name.Contains("State") &&
                   (type.Interfaces.Any(i => i.Contains("State")) ||
                    type.BaseClass?.Contains("State") == true);
        }

        private bool IsEntryPoint(TypeInfo type)
        {
            return type.HasRuntimeInitialize ||
                   type.Name.Contains("Bootstrap") ||
                   type.Name.Contains("Entry") ||
                   type.Name.Contains("Startup") ||
                   type.Name.Contains("Installer") ||
                   type.Name == "Project";
        }

        private void DetectCodeSmells(TypeInfo type, List<TypeInfo> allTypes)
        {
            if (type.LinesOfCode > GodClassLineThreshold || type.FieldCount > GodClassFieldThreshold)
            {
                type.Problems.Add(ProblemType.GodClass);
            }

            if (type.Dependencies.Count > HighCouplingThreshold)
            {
                type.Problems.Add(ProblemType.HighCoupling);
            }

            if (IsOrphan(type, allTypes))
            {
                type.Problems.Add(ProblemType.Orphan);
            }
        }

        private bool IsOrphan(TypeInfo type, List<TypeInfo> allTypes)
        {
            if (type.IsEntryPoint || type.Kind == TypeKind.Interface) return false;

            return !allTypes.Any(t => t.Name != type.Name &&
                (t.Dependencies.Contains(type.Name) ||
                 t.Interfaces.Contains(type.Name) ||
                 t.BaseClass == type.Name));
        }

        private void CalculateUsedByRelationships(List<TypeInfo> types)
        {
            foreach (var type in types)
            {
                foreach (var dependency in type.Dependencies)
                {
                    var dependencyType = types.FirstOrDefault(t => t.Name == dependency);
                    if (dependencyType != null && !dependencyType.UsedByNames.Contains(type.Name))
                    {
                        dependencyType.UsedByNames.Add(type.Name);
                    }
                }
            }
        }

        private void CreateFolderGroups(List<TypeInfo> types)
        {
            var allFolders = CollectAllFolderPaths(types);

            foreach (var folderPath in allFolders.OrderBy(f => f.Split('/').Length).ThenBy(f => f))
            {
                var depth = folderPath.Split('/').Length - 1;
                var borderColor = DepthColors[depth % DepthColors.Length];

                var group = CreateFolderGroup(folderPath, borderColor);
                AddElement(group);
                _groupMap[folderPath] = group;
            }
        }

        private HashSet<string> CollectAllFolderPaths(List<TypeInfo> types)
        {
            var folders = new HashSet<string>();

            foreach (var type in types)
            {
                var folder = type.FolderGroup ?? "Root";
                folders.Add(folder);

                var parts = folder.Split('/');
                for (int i = 1; i < parts.Length; i++)
                {
                    folders.Add(string.Join("/", parts.Take(i)));
                }
            }

            return folders;
        }

        private Group CreateFolderGroup(string folderPath, Color borderColor)
        {
            var group = new Group { title = $"[{folderPath}]" };

            group.style.backgroundColor = new Color(
                borderColor.r * 0.1f,
                borderColor.g * 0.1f,
                borderColor.b * 0.1f,
                0.9f
            );

            ApplyGroupBorderStyle(group, borderColor);

            return group;
        }

        private void ApplyGroupBorderStyle(Group group, Color color)
        {
            group.style.borderTopWidth = 2;
            group.style.borderBottomWidth = 2;
            group.style.borderLeftWidth = 2;
            group.style.borderRightWidth = 2;

            group.style.borderTopColor = color;
            group.style.borderBottomColor = color;
            group.style.borderLeftColor = color;
            group.style.borderRightColor = color;

            group.style.borderTopLeftRadius = 8;
            group.style.borderTopRightRadius = 8;
            group.style.borderBottomLeftRadius = 8;
            group.style.borderBottomRightRadius = 8;
        }

        private void CreateNodes(List<TypeInfo> types)
        {
            foreach (var type in types)
            {
                var node = new TypeNode(type);
                AddElement(node);
                _nodeMap[type.Name] = node;

                AssignNodeToGroup(node, type);
                TrackSpecialNodes(node, type);
                RegisterNodeCallbacks(node);
            }
        }

        private void AssignNodeToGroup(TypeNode node, TypeInfo type)
        {
            if (_groupMap.TryGetValue(type.FolderGroup ?? "Root", out var group))
            {
                group.AddElement(node);
            }
        }

        private void TrackSpecialNodes(TypeNode node, TypeInfo type)
        {
            if (type.IsEntryPoint)
            {
                _entryPoints.Add(node);
            }

            if (type.Problems.Count > 0)
            {
                _problemNodes.Add(node);
            }
        }

        private void RegisterNodeCallbacks(TypeNode node)
        {
            node.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 1 && evt.button == 0)
                {
                    _selectedNode = node;
                    HighlightNodeConnections(node);
                }
            });

            node.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (_selectedNode == null)
                {
                    HighlightNodeConnections(node);
                }
            });

            node.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (_selectedNode == null)
                {
                    ResetHighlighting();
                }
            });
        }

        private void CreateEdges(List<TypeInfo> types)
        {
            foreach (var type in types)
            {
                if (!_nodeMap.TryGetValue(type.Name, out var sourceNode)) continue;

                CreateInheritanceEdges(type, sourceNode);
                CreateInterfaceEdges(type, sourceNode);
                CreateDependencyEdges(type, sourceNode);
            }
        }

        private void CreateInheritanceEdges(TypeInfo type, TypeNode sourceNode)
        {
            if (string.IsNullOrEmpty(type.BaseClass)) return;

            if (_nodeMap.TryGetValue(type.BaseClass, out var baseNode))
            {
                CreateEdge(sourceNode.OutputPort, baseNode.InputPort, false, false);
            }
        }

        private void CreateInterfaceEdges(TypeInfo type, TypeNode sourceNode)
        {
            foreach (var interfaceName in type.Interfaces)
            {
                if (_nodeMap.TryGetValue(interfaceName, out var interfaceNode))
                {
                    CreateEdge(sourceNode.OutputPort, interfaceNode.InputPort, false, false);
                }
            }
        }

        private void CreateDependencyEdges(TypeInfo type, TypeNode sourceNode)
        {
            foreach (var dependency in type.Dependencies.Distinct())
            {
                if (dependency == type.Name) continue;
                if (!_nodeMap.TryGetValue(dependency, out var dependencyNode)) continue;

                var binding = _diBindings.FirstOrDefault(b => b.InterfaceType == dependency);

                if (binding != null && _nodeMap.TryGetValue(binding.ImplementationType, out var implementationNode))
                {
                    CreateEdge(sourceNode.DependencyPort, implementationNode.InputPort, false, true);
                }
                else
                {
                    bool isCircular = _circularPairs.Contains(CreatePairKey(type.Name, dependency));
                    CreateEdge(sourceNode.DependencyPort, dependencyNode.InputPort, isCircular, false);
                }
            }
        }

        private void CreateEdge(Port output, Port input, bool isCircular, bool isDIBinding)
        {
            var edge = new Edge
            {
                output = output,
                input = input
            };

            edge.input.Connect(edge);
            edge.output.Connect(edge);
            edge.style.display = DisplayStyle.None;

            if (isCircular)
            {
                edge.style.unityBackgroundImageTintColor = new Color(1f, 0.2f, 0.2f);
            }
            else if (isDIBinding)
            {
                edge.style.unityBackgroundImageTintColor = new Color(0.5f, 0.8f, 1f);
                edge.AddToClassList("dashed");
            }

            AddElement(edge);
            _allEdges.Add(edge);
        }

        public void AutoLayout()
        {
            var nodesList = nodes.ToList().OfType<TypeNode>().ToList();
            if (nodesList.Count == 0) return;

            var folderHierarchy = BuildFolderHierarchy();

            float currentY = StartY;
            foreach (var rootFolder in folderHierarchy[""].OrderBy(f => f))
            {
                var rect = LayoutFolder(rootFolder, folderHierarchy, nodesList, StartX, currentY);
                currentY = rect.yMax + GroupSpacing;
            }

            foreach (var group in _groupMap.Values)
            {
                group.UpdateGeometryFromContent();
            }

            FrameAll();
        }

        private Dictionary<string, List<string>> BuildFolderHierarchy()
        {
            var folderHierarchy = new Dictionary<string, List<string>> { [""] = new List<string>() };

            foreach (var folder in _groupMap.Keys)
            {
                var parentPath = GetParentPath(folder);
                if (!folderHierarchy.ContainsKey(parentPath))
                {
                    folderHierarchy[parentPath] = new List<string>();
                }
                folderHierarchy[parentPath].Add(folder);
            }

            return folderHierarchy;
        }

        private Rect LayoutFolder(
            string folder,
            Dictionary<string, List<string>> folderHierarchy,
            List<TypeNode> allNodes,
            float x,
            float y)
        {
            var folderNodes = GetNodesForFolder(folder, allNodes);
            var childFolders = folderHierarchy.ContainsKey(folder) ? folderHierarchy[folder] : new List<string>();

            float contentX = x + GroupPadding;
            float contentY = y + GroupPadding + 25;
            float maxWidth = 0f;

            var nodeLayoutResult = LayoutNodes(folderNodes, contentX, contentY);
            contentY = nodeLayoutResult.y;
            maxWidth = nodeLayoutResult.width;

            float childY = contentY;
            float childMaxWidth = 0f;

            foreach (var child in childFolders.OrderBy(c => c))
            {
                var rect = LayoutFolder(child, folderHierarchy, allNodes, contentX, childY);
                childY = rect.yMax + 30f;
                childMaxWidth = Math.Max(childMaxWidth, rect.width);
                maxWidth = Math.Max(maxWidth, rect.width);
            }

            float totalHeight = childY - y + GroupPadding;
            float totalWidth = Math.Max(Math.Max(maxWidth + GroupPadding * 2, childMaxWidth), MinGroupWidth);

            return new Rect(x, y, totalWidth, Math.Max(totalHeight, MinGroupHeight));
        }

        private List<TypeNode> GetNodesForFolder(string folder, List<TypeNode> allNodes)
        {
            return allNodes
                .Where(n => (n.TypeInfo.FolderGroup ?? "Root") == folder)
                .OrderBy(n => GetTypeSortOrder(n.TypeInfo.Kind))
                .ThenBy(n => n.TypeInfo.Name)
                .ToList();
        }

        private (float y, float width) LayoutNodes(List<TypeNode> nodesList, float startX, float startY)
        {
            if (nodesList.Count == 0) return (startY, 0f);

            int cols = Math.Max(1, Math.Min(MaxNodesPerRow, nodesList.Count));
            int col = 0;
            float currentY = startY;
            float rowHeight = 0f;
            float maxWidth = 0f;
            TypeKind? previousKind = null;

            foreach (var node in nodesList)
            {
                if (previousKind.HasValue && previousKind.Value != node.TypeInfo.Kind && col > 0)
                {
                    col = 0;
                    currentY += rowHeight + NodeVerticalGap;
                    rowHeight = 0f;
                }

                float nodeWidth = node.NodeWidth;
                float nodeHeight = node.NodeHeight;

                node.SetPosition(new Rect(
                    startX + col * (nodeWidth + NodeHorizontalGap),
                    currentY,
                    nodeWidth,
                    nodeHeight
                ));

                rowHeight = Math.Max(rowHeight, nodeHeight);
                col++;
                previousKind = node.TypeInfo.Kind;

                if (col >= cols)
                {
                    col = 0;
                    currentY += rowHeight + NodeVerticalGap;
                    rowHeight = 0f;
                }
            }

            if (nodesList.Count > 0)
            {
                maxWidth = cols * (nodesList.Max(n => n.NodeWidth) + NodeHorizontalGap);
                if (col != 0)
                {
                    currentY += rowHeight + NodeVerticalGap;
                }
            }

            return (currentY, maxWidth);
        }

        private void HighlightNodeConnections(TypeNode node)
        {
            if (node == null) return;

            DimAllElements();

            node.style.opacity = 1f;

            var connectedNodes = new HashSet<TypeNode> { node };

            foreach (var edge in _allEdges)
            {
                var sourceNode = edge.output?.node as TypeNode;
                var targetNode = edge.input?.node as TypeNode;

                if (sourceNode == node || targetNode == node)
                {
                    edge.style.display = DisplayStyle.Flex;
                    if (sourceNode != null) connectedNodes.Add(sourceNode);
                    if (targetNode != null) connectedNodes.Add(targetNode);
                }
            }

            foreach (var connectedNode in connectedNodes)
            {
                connectedNode.style.opacity = 1f;
            }
        }

        private void DimAllElements()
        {
            foreach (var node in nodes.ToList().OfType<TypeNode>())
            {
                node.style.opacity = 0.15f;
            }

            foreach (var edge in _allEdges)
            {
                edge.style.display = DisplayStyle.None;
            }

            foreach (var group in _groupMap.Values)
            {
                group.style.opacity = 0.3f;
            }
        }

        private void FocusSpecificNodes(List<TypeNode> nodesToFocus)
        {
            foreach (var node in nodes.ToList().OfType<TypeNode>())
            {
                node.style.opacity = nodesToFocus.Contains(node) ? 1f : 0.15f;
            }

            foreach (var group in _groupMap.Values)
            {
                group.style.opacity = 0.3f;
            }

            foreach (var edge in _allEdges)
            {
                edge.style.display = DisplayStyle.None;
            }
        }

        private void ShowCircularDependencyEdges()
        {
            foreach (var edge in _allEdges)
            {
                var sourceNode = edge.output?.node as TypeNode;
                var targetNode = edge.input?.node as TypeNode;

                if (sourceNode != null && targetNode != null &&
                    _circularPairs.Contains(CreatePairKey(sourceNode.TypeInfo.Name, targetNode.TypeInfo.Name)))
                {
                    edge.style.display = DisplayStyle.Flex;
                }
                else
                {
                    edge.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdateWindowStats(List<TypeInfo> types, int totalFiles)
        {
            var stats = new AnalysisStats
            {
                ClassCount = types.Count(t => t.Kind == TypeKind.Class),
                InterfaceCount = types.Count(t => t.Kind == TypeKind.Interface),
                MonoBehaviourCount = types.Count(t => t.Kind == TypeKind.MonoBehaviour),
                ScriptableObjectCount = types.Count(t => t.Kind == TypeKind.ScriptableObject),
                StaticClassCount = types.Count(t => t.Kind == TypeKind.StaticClass),
                TotalFiles = totalFiles,
                EntryPointCount = _entryPoints.Count,
                AvgDependencies = types.Count > 0 ? (float)types.Sum(t => t.Dependencies.Count) / types.Count : 0,
                CircularCount = _circularPairs.Count,
                GodClassCount = types.Count(t => t.Problems.Contains(ProblemType.GodClass)),
                OrphanCount = types.Count(t => t.Problems.Contains(ProblemType.Orphan)),
                HighCouplingCount = types.Count(t => t.Problems.Contains(ProblemType.HighCoupling))
            };

            _window.UpdateStats(stats);
        }

        private string CreatePairKey(string a, string b)
        {
            return string.Compare(a, b, StringComparison.Ordinal) < 0 ? $"{a}<->{b}" : $"{b}<->{a}";
        }

        private string GetParentPath(string path)
        {
            var index = path.LastIndexOf('/');
            return index > 0 ? path.Substring(0, index) : "";
        }

        private int GetTypeSortOrder(TypeKind kind)
        {
            return kind switch
            {
                TypeKind.Interface => 0,
                TypeKind.MonoBehaviour => 1,
                TypeKind.ScriptableObject => 2,
                TypeKind.StaticClass => 3,
                TypeKind.Class => 4,
                _ => 5
            };
        }
    }

    public class TypeNode : Node
    {
        private const float BaseNodeWidth = 180f;
        private const float BaseNodeHeight = 100f;
        private const float MaxSizeScale = 2.5f;
        private const float MinSizeScale = 1f;
        private const int LinesOfCodeScaleDivisor = 150;
        private const int FieldCountScaleDivisor = 8;
        private const int MaxDependenciesToShow = 15;

        public TypeInfo TypeInfo { get; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }
        public Port DependencyPort { get; private set; }
        public float NodeWidth { get; }
        public float NodeHeight { get; }

        public TypeNode(TypeInfo info)
        {
            TypeInfo = info ?? throw new ArgumentNullException(nameof(info));

            CalculateNodeSize(out var width, out var height);
            NodeWidth = width;
            NodeHeight = height;

            title = info.Name;

            StyleTitle(info);
            AddKindLabel(info);
            AddBadges(info);
            CreatePorts();
            AddStatsDisplay(info);
            SetupTooltip(info);
            RegisterDoubleClickHandler(info);

            RefreshExpandedState();
            RefreshPorts();
        }

        private void CalculateNodeSize(out float width, out float height)
        {
            float locScale = Mathf.Clamp(
                TypeInfo.LinesOfCode / (float)LinesOfCodeScaleDivisor,
                MinSizeScale,
                MaxSizeScale
            );

            float fieldScale = Mathf.Clamp(
                TypeInfo.FieldCount / (float)FieldCountScaleDivisor,
                MinSizeScale,
                2f
            );

            float scale = Mathf.Max(locScale, fieldScale);
            width = BaseNodeWidth * scale;
            height = BaseNodeHeight * scale;
        }

        private void StyleTitle(TypeInfo info)
        {
            Color titleColor = GetTitleColor(info);
            titleContainer.style.backgroundColor = titleColor;
        }

        private Color GetTitleColor(TypeInfo info)
        {
            if (info.Problems.Contains(ProblemType.CircularDependency))
            {
                return new Color(0.85f, 0.25f, 0.25f);
            }

            if (info.Problems.Contains(ProblemType.GodClass))
            {
                return new Color(0.85f, 0.65f, 0.1f);
            }

            return info.Kind switch
            {
                TypeKind.Interface => new Color(0.25f, 0.6f, 0.25f),
                TypeKind.MonoBehaviour => new Color(0.2f, 0.4f, 0.7f),
                TypeKind.ScriptableObject => new Color(0.7f, 0.45f, 0.2f),
                TypeKind.StaticClass => new Color(0.5f, 0.3f, 0.5f),
                _ => new Color(0.4f, 0.4f, 0.4f)
            };
        }

        private void AddKindLabel(TypeInfo info)
        {
            var kindLabel = new Label($"«{info.Kind}»")
            {
                style =
                {
                    fontSize = 9,
                    color = new Color(1f, 1f, 1f, 0.85f),
                    unityFontStyleAndWeight = FontStyle.Italic,
                    marginLeft = 4
                }
            };
            titleContainer.Add(kindLabel);
        }

        private void AddBadges(TypeInfo info)
        {
            var badgeContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    paddingLeft = 4,
                    paddingTop = 2,
                    paddingBottom = 2
                }
            };
            mainContainer.Insert(1, badgeContainer);

            if (info.IsEntryPoint)
            {
                AddBadge(badgeContainer, "ENTRY", new Color(0.15f, 0.5f, 0.15f));
            }

            foreach (var pattern in info.DetectedPatterns)
            {
                AddBadge(badgeContainer, pattern, new Color(0.25f, 0.45f, 0.65f));
            }

            AddProblemBadges(badgeContainer, info);
        }

        private void AddProblemBadges(VisualElement container, TypeInfo info)
        {
            if (info.Problems.Contains(ProblemType.GodClass))
            {
                AddBadge(container, "GOD", new Color(0.65f, 0.5f, 0.1f));
            }

            if (info.Problems.Contains(ProblemType.Orphan))
            {
                AddBadge(container, "ORPHAN", new Color(0.65f, 0.4f, 0.1f));
            }

            if (info.Problems.Contains(ProblemType.HighCoupling))
            {
                AddBadge(container, "COUPLED", new Color(0.25f, 0.35f, 0.65f));
            }

            if (info.Problems.Contains(ProblemType.CircularDependency))
            {
                AddBadge(container, "CIRCULAR", new Color(0.65f, 0.2f, 0.2f));
            }
        }

        private void AddBadge(VisualElement container, string text, Color backgroundColor)
        {
            var badge = new Label(text)
            {
                style =
                {
                    fontSize = 8,
                    color = Color.white,
                    backgroundColor = backgroundColor,
                    paddingLeft = 3,
                    paddingRight = 3,
                    paddingTop = 1,
                    paddingBottom = 1,
                    marginRight = 2,
                    marginBottom = 1,
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2
                }
            };
            container.Add(badge);
        }

        private void CreatePorts()
        {
            InputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "";
            inputContainer.Add(InputPort);

            OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
            OutputPort.portName = "Extends";
            outputContainer.Add(OutputPort);

            DependencyPort = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(float));
            DependencyPort.portName = "Uses";
            DependencyPort.portColor = new Color(1f, 0.6f, 0.2f);
            outputContainer.Add(DependencyPort);
        }

        private void AddStatsDisplay(TypeInfo info)
        {
            var statsContainer = new VisualElement
            {
                style =
                {
                    paddingLeft = 6,
                    paddingRight = 6,
                    paddingTop = 3,
                    paddingBottom = 3,
                    backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.85f)
                }
            };

            var statsLabel = new Label($"LOC:{info.LinesOfCode} | Fld:{info.FieldCount} | Mtd:{info.MethodCount}")
            {
                style =
                {
                    fontSize = 9,
                    color = new Color(0.55f, 0.55f, 0.55f)
                }
            };

            statsContainer.Add(statsLabel);
            extensionContainer.Add(statsContainer);
        }

        private void SetupTooltip(TypeInfo info)
        {
            var dependenciesText = FormatListForTooltip(info.Dependencies, "none");
            var usedByText = FormatListForTooltip(info.UsedByNames, "none");

            tooltip = $"{info.FolderGroup}/{info.Name}\n" +
                      $"------------------------\n" +
                      $"LOC: {info.LinesOfCode} | Fields: {info.FieldCount} | Methods: {info.MethodCount}\n" +
                      $"------------------------\n" +
                      $"Depends on ({info.Dependencies.Count}):\n{dependenciesText}\n\n" +
                      $"Used by ({info.UsedByNames.Count}):\n{usedByText}";

            if (info.DetectedPatterns.Count > 0)
            {
                tooltip += $"\n\nPatterns: {string.Join(", ", info.DetectedPatterns)}";
            }
        }

        private string FormatListForTooltip(List<string> items, string emptyText)
        {
            if (!items.Any()) return emptyText;

            var result = string.Join(", ", items.Take(MaxDependenciesToShow));
            if (items.Count > MaxDependenciesToShow)
            {
                result += $" +{items.Count - MaxDependenciesToShow}";
            }

            return result;
        }

        private void RegisterDoubleClickHandler(TypeInfo info)
        {
            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && !string.IsNullOrEmpty(info.FilePath))
                {
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(info.FilePath, 1);
                    evt.StopPropagation();
                }
            });
        }
    }

    public enum TypeKind
    {
        Class,
        Interface,
        MonoBehaviour,
        ScriptableObject,
        StaticClass
    }

    public enum ProblemType
    {
        CircularDependency,
        GodClass,
        Orphan,
        HighCoupling
    }

    public class TypeInfo
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string FilePath { get; set; }
        public string FolderGroup { get; set; }
        public string BaseClass { get; set; }
        public TypeKind Kind { get; set; }

        public List<string> Interfaces { get; set; } = new List<string>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public List<string> Fields { get; set; } = new List<string>();
        public List<string> DetectedPatterns { get; set; } = new List<string>();
        public List<ProblemType> Problems { get; set; } = new List<ProblemType>();
        public List<string> UsedByNames { get; set; } = new List<string>();

        public int LinesOfCode { get; set; }
        public int FieldCount { get; set; }
        public int MethodCount { get; set; }

        public bool HasPrivateConstructor { get; set; }
        public bool HasStaticInstance { get; set; }
        public bool HasCreateMethod { get; set; }
        public bool HasEventFields { get; set; }
        public bool HasRuntimeInitialize { get; set; }
        public bool IsEntryPoint { get; set; }
    }

    public class DIBinding
    {
        public string InterfaceType { get; set; }
        public string ImplementationType { get; set; }
    }

    public class CodeAnalyzer
    {
        private const string CSharpFilePattern = "*.cs";
        private const string EditorFolderName = "/Editor/";

        private static readonly Regex ClassRegex = new Regex(
            @"(?:public|private|protected|internal)\s+(?:abstract\s+|sealed\s+|static\s+|partial\s+)*(?:class|struct)\s+(\w+)(?:\s*<[^>]+>)?(?:\s*:\s*([^{]+))?",
            RegexOptions.Compiled
        );

        private static readonly Regex InterfaceRegex = new Regex(
            @"(?:public|private|protected|internal)\s+interface\s+(\w+)(?:\s*<[^>]+>)?(?:\s*:\s*([^{]+))?",
            RegexOptions.Compiled
        );

        private static readonly Regex NamespaceRegex = new Regex(
            @"namespace\s+([\w.]+)",
            RegexOptions.Compiled
        );

        private static readonly Regex FieldRegex = new Regex(
            @"(?:private|public|protected|internal)\s+(?:readonly\s+)?(?:static\s+)?(\w+(?:<[^>]+>)?)\s+_?(\w+)\s*[;=]",
            RegexOptions.Compiled
        );

        private static readonly Regex MethodRegex = new Regex(
            @"(?:private|public|protected|internal|protected\s+internal)\s+(?:static\s+|virtual\s+|override\s+|abstract\s+|async\s+)*(?:\w+(?:<[^>]+>)?)\s+(\w+)\s*\(",
            RegexOptions.Compiled
        );

        private static readonly Regex ConstructorParamRegex = new Regex(
            @"(?:public|private|protected|internal)\s+(\w+)\s*\(([^)]*)\)",
            RegexOptions.Compiled
        );

        private readonly HashSet<string> _unityTypes = new HashSet<string>
        {
            "MonoBehaviour", "ScriptableObject", "Component", "GameObject", "Transform",
            "Rigidbody", "Collider", "Camera", "AudioSource", "Animator",
            "Vector2", "Vector3", "Vector4", "Quaternion", "Color", "Rect"
        };

        private readonly HashSet<string> _systemTypes = new HashSet<string>
        {
            "string", "String", "int", "Int32", "float", "Single", "double", "Double",
            "bool", "Boolean", "void", "object", "Object", "byte",
            "List", "Dictionary", "HashSet", "Queue", "Stack", "Array",
            "Action", "Func", "Task", "UniTask", "CancellationToken",
            "IEnumerable", "IEnumerator", "IList", "IDictionary", "IDisposable"
        };

        public int TotalFilesScanned { get; private set; }
        public List<DIBinding> DIBindings { get; private set; } = new List<DIBinding>();

        public List<TypeInfo> AnalyzeDirectory(string path, bool analyzeDI = false)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Directory not found: {path}");
            }

            var types = new List<TypeInfo>();
            var files = GetCSharpFiles(path);
            TotalFilesScanned = files.Length;

            foreach (var file in files)
            {
                try
                {
                    types.AddRange(AnalyzeFile(file, path));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to analyze {file}: {e.Message}");
                }
            }

            CleanupDependencies(types);

            if (analyzeDI)
            {
                DIBindings = ParseDIBindings(files);
                if (DIBindings.Count > 0)
                {
                    Debug.Log($"[Architecture Visualizer] Found {DIBindings.Count} DI binding(s)");
                }
            }

            return types;
        }

        private string[] GetCSharpFiles(string path)
        {
            return Directory.GetFiles(path, CSharpFilePattern, SearchOption.AllDirectories)
                .Where(f => !f.Replace("\\", "/").Contains(EditorFolderName))
                .ToArray();
        }

        private List<TypeInfo> AnalyzeFile(string filePath, string basePath)
        {
            var types = new List<TypeInfo>();
            var content = File.ReadAllText(filePath);
            var folder = ExtractFolderPath(filePath, basePath);

            var fileMetadata = ExtractFileMetadata(content);
            var cleanContent = RemoveComments(content);
            var namespaceName = ExtractNamespace(cleanContent);

            types.AddRange(ExtractInterfaces(cleanContent, namespaceName, filePath, folder));
            types.AddRange(ExtractClasses(cleanContent, namespaceName, filePath, folder, fileMetadata));

            return types;
        }

        private FileMetadata ExtractFileMetadata(string content)
        {
            return new FileMetadata
            {
                HasRuntimeInitialize = content.Contains("[RuntimeInitializeOnLoadMethod"),
                HasEventFields = Regex.IsMatch(content, @"event\s+|Action<|UnityEvent|UnityAction"),
                HasCreateMethod = Regex.IsMatch(content, @"static\s+\w+\s+Create"),
                HasStaticInstance = Regex.IsMatch(content, @"static\s+\w+\s+(?:Instance|_instance)")
            };
        }

        private string RemoveComments(string content)
        {
            var withoutSingleLine = Regex.Replace(content, @"//.*?$", "", RegexOptions.Multiline);
            var withoutMultiLine = Regex.Replace(withoutSingleLine, @"/\*.*?\*/", "", RegexOptions.Singleline);
            return withoutMultiLine;
        }

        private string ExtractNamespace(string content)
        {
            var match = NamespaceRegex.Match(content);
            return match.Success ? match.Groups[1].Value : null;
        }

        private List<TypeInfo> ExtractInterfaces(string content, string namespaceName, string filePath, string folder)
        {
            var interfaces = new List<TypeInfo>();

            foreach (Match match in InterfaceRegex.Matches(content))
            {
                var interfaceInfo = new TypeInfo
                {
                    Name = match.Groups[1].Value,
                    Namespace = namespaceName,
                    FilePath = filePath,
                    FolderGroup = folder,
                    Kind = TypeKind.Interface,
                    LinesOfCode = File.ReadAllLines(filePath).Length
                };

                if (match.Groups[2].Success)
                {
                    interfaceInfo.Interfaces.AddRange(
                        ParseBaseTypes(match.Groups[2].Value)
                            .Where(IsInterfaceName)
                    );
                }

                interfaces.Add(interfaceInfo);
            }

            return interfaces;
        }

        private List<TypeInfo> ExtractClasses(
            string content,
            string namespaceName,
            string filePath,
            string folder,
            FileMetadata metadata)
        {
            var classes = new List<TypeInfo>();

            foreach (Match match in ClassRegex.Matches(content))
            {
                var className = match.Groups[1].Value;
                var body = ExtractTypeBody(content, match.Index);

                var classInfo = CreateClassInfo(
                    className,
                    namespaceName,
                    filePath,
                    folder,
                    body,
                    metadata
                );

                if (match.Value.Contains("static class"))
                {
                    classInfo.Kind = TypeKind.StaticClass;
                }

                if (match.Groups[2].Success)
                {
                    ProcessBaseTypes(classInfo, match.Groups[2].Value);
                }

                if (body != null)
                {
                    AnalyzeClassBody(classInfo, body, className);
                }

                classes.Add(classInfo);
            }

            return classes;
        }

        private TypeInfo CreateClassInfo(
            string name,
            string namespaceName,
            string filePath,
            string folder,
            string body,
            FileMetadata metadata)
        {
            return new TypeInfo
            {
                Name = name,
                Namespace = namespaceName,
                FilePath = filePath,
                FolderGroup = folder,
                Kind = TypeKind.Class,
                LinesOfCode = body?.Split('\n').Length ?? File.ReadAllLines(filePath).Length,
                HasRuntimeInitialize = metadata.HasRuntimeInitialize,
                HasEventFields = metadata.HasEventFields,
                HasCreateMethod = metadata.HasCreateMethod,
                HasStaticInstance = metadata.HasStaticInstance,
                HasPrivateConstructor = body != null && Regex.IsMatch(body, $@"private\s+{name}\s*\(\s*\)")
            };
        }

        private void ProcessBaseTypes(TypeInfo classInfo, string baseTypesString)
        {
            foreach (var baseType in ParseBaseTypes(baseTypesString))
            {
                if (baseType == "MonoBehaviour")
                {
                    classInfo.Kind = TypeKind.MonoBehaviour;
                }
                else if (baseType == "ScriptableObject")
                {
                    classInfo.Kind = TypeKind.ScriptableObject;
                }
                else if (IsInterfaceName(baseType))
                {
                    classInfo.Interfaces.Add(baseType);
                }
                else if (!_unityTypes.Contains(baseType) && !_systemTypes.Contains(baseType))
                {
                    classInfo.BaseClass = baseType;
                }
            }
        }

        private void AnalyzeClassBody(TypeInfo classInfo, string body, string className)
        {
            AnalyzeFields(classInfo, body);
            classInfo.MethodCount = MethodRegex.Matches(body).Count;
            AnalyzeConstructorParameters(classInfo, body, className);
        }

        private void AnalyzeFields(TypeInfo classInfo, string body)
        {
            var fieldMatches = FieldRegex.Matches(body);
            classInfo.FieldCount = fieldMatches.Count;

            foreach (Match fieldMatch in fieldMatches)
            {
                var fieldType = CleanTypeName(fieldMatch.Groups[1].Value);
                var fieldName = fieldMatch.Groups[2].Value;

                classInfo.Fields.Add($"{fieldType} {fieldName}");
                AddDependencyIfValid(classInfo, fieldType);
            }
        }

        private void AnalyzeConstructorParameters(TypeInfo classInfo, string body, string className)
        {
            foreach (Match constructorMatch in ConstructorParamRegex.Matches(body))
            {
                if (constructorMatch.Groups[1].Value != className) continue;

                var parameters = constructorMatch.Groups[2].Value;
                foreach (Match paramMatch in Regex.Matches(parameters, @"(\w+(?:<[^>]+>)?)\s+\w+"))
                {
                    var paramType = CleanTypeName(paramMatch.Groups[1].Value);
                    AddDependencyIfValid(classInfo, paramType);
                }
            }

            var initMethodPattern = @"(?:public|private|protected|internal)\s+(?:void|UniTask)\s+(Initialize|Construct|Setup|Inject)\s*\(([^)]*)\)";
            foreach (Match initMatch in Regex.Matches(body, initMethodPattern))
            {
                var parameters = initMatch.Groups[2].Value;
                foreach (Match paramMatch in Regex.Matches(parameters, @"(\w+(?:<[^>]+>)?)\s+\w+"))
                {
                    var paramType = CleanTypeName(paramMatch.Groups[1].Value);
                    AddDependencyIfValid(classInfo, paramType);
                }
            }
        }

        private string ExtractFolderPath(string filePath, string basePath)
        {
            var relativePath = filePath
                .Replace("\\", "/")
                .Replace(basePath.Replace("\\", "/"), "")
                .TrimStart('/');

            var parts = relativePath.Split('/');
            return parts.Length > 1 ? string.Join("/", parts.Take(parts.Length - 1)) : "Root";
        }

        private List<string> ParseBaseTypes(string baseTypesString)
        {
            return baseTypesString
                .Split(',')
                .Select(x => CleanTypeName(x.Trim()))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();
        }

        private string CleanTypeName(string typeName)
        {
            return Regex.Replace(typeName, @"<.*>", "")
                .Replace("[]", "")
                .Replace("?", "")
                .Trim();
        }

        private bool IsInterfaceName(string name)
        {
            return name.StartsWith("I") && name.Length > 1 && char.IsUpper(name[1]);
        }

        private void AddDependencyIfValid(TypeInfo typeInfo, string dependencyType)
        {
            if (string.IsNullOrEmpty(dependencyType)) return;
            if (_systemTypes.Contains(dependencyType)) return;
            if (_unityTypes.Contains(dependencyType)) return;
            if (dependencyType == typeInfo.Name) return;

            typeInfo.Dependencies.Add(dependencyType);
        }

        private string ExtractTypeBody(string content, int startIndex)
        {
            var openBraceIndex = content.IndexOf('{', startIndex);
            if (openBraceIndex == -1) return null;

            int depth = 1;
            int i = openBraceIndex + 1;

            while (i < content.Length && depth > 0)
            {
                if (content[i] == '{') depth++;
                else if (content[i] == '}') depth--;
                i++;
            }

            return content.Substring(openBraceIndex, i - openBraceIndex);
        }

        private void CleanupDependencies(List<TypeInfo> types)
        {
            var validTypeNames = new HashSet<string>(types.Select(t => t.Name));

            foreach (var type in types)
            {
                type.Dependencies = type.Dependencies
                    .Where(d => validTypeNames.Contains(d) && d != type.Name)
                    .Distinct()
                    .ToList();

                type.Interfaces = type.Interfaces
                    .Where(i => validTypeNames.Contains(i))
                    .ToList();

                if (!string.IsNullOrEmpty(type.BaseClass) &&
                    !validTypeNames.Contains(type.BaseClass) &&
                    !_unityTypes.Contains(type.BaseClass))
                {
                    type.BaseClass = null;
                }
            }
        }

        private List<DIBinding> ParseDIBindings(string[] files)
        {
            var bindings = new List<DIBinding>();

            var patterns = new[]
            {
                @"Register<(\w+),\s*(\w+)>",
                @"RegisterInstance<(\w+),\s*(\w+)>",
                @"Bind<(\w+)>\(\)\.To<(\w+)>",
                @"Register<(\w+)>.*\.As<(\w+)>",
                @"AddTransient<(\w+),\s*(\w+)>",
                @"AddScoped<(\w+),\s*(\w+)>",
                @"AddSingleton<(\w+),\s*(\w+)>"
            };

            foreach (var file in files)
            {
                try
                {
                    var content = File.ReadAllText(file);

                    foreach (var pattern in patterns)
                    {
                        var matches = Regex.Matches(content, pattern);
                        foreach (Match match in matches)
                        {
                            if (match.Groups.Count >= 3)
                            {
                                var interfaceType = match.Groups[1].Value;
                                var implementationType = match.Groups[2].Value;

                                if (!string.IsNullOrEmpty(interfaceType) && !string.IsNullOrEmpty(implementationType))
                                {
                                    bindings.Add(new DIBinding
                                    {
                                        InterfaceType = interfaceType,
                                        ImplementationType = implementationType
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse DI bindings from {file}: {e.Message}");
                }
            }

            return bindings;
        }

        private class FileMetadata
        {
            public bool HasRuntimeInitialize { get; set; }
            public bool HasEventFields { get; set; }
            public bool HasCreateMethod { get; set; }
            public bool HasStaticInstance { get; set; }
        }
    }
}
