using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using ET;
using Unity.Mathematics;

// -------------------- Node --------------------
public class ETNode
{
    public Rect rect;
    public string title;
    public Color color;

    public ETNode(Vector2 position, Vector2 size, string title, Color color)
    {
        this.rect = new Rect(position, size);
        this.title = title;
        this.color = color;
    }

    public void Draw(GUIStyle style, Texture2D tex, Rect screenRect)
    {
        style.normal.background = tex;
        style.normal.textColor = Color.black;
        GUI.Box(screenRect, title, style);
    }
    
    public Vector2 GetLeftCenter() => new(rect.x, rect.y + rect.height / 2f);
    public Vector2 GetRightCenter() => new(rect.x + rect.width, rect.y + rect.height / 2f);
}

// -------------------- Connection --------------------
public class Connection
{
    public ETNode from;
    public ETNode to;

    public Connection(ETNode from, ETNode to)
    {
        this.from = from;
        this.to = to;
    }
}

// -------------------- SimpleNodeGraph --------------------
public class ETSceneNodeGraph: EditorWindow
{
    private Vector2 viewOffset = Vector2.zero;
    private bool isDragging = false;
    private float zoomLevel = 1.0f;

    private const float MinZoom = 0.2f;
    private const float MaxZoom = 3.0f;

    private static Dictionary<Color, Texture2D> textureCache;
    private GUIStyle nodeStyle;

    private readonly List<NodeData> nodeDataList = new();
    private int maxIndexX;
    private Vector2 startPos = new Vector2(50, 50);
    public class NodeData
    {
        public readonly List<ETNode> NodeList = new();
        public readonly List<Connection> ConnectionList = new();
        public ETNode StartNode;
        public Vector2 StartPos = new Vector2(50, 50);
        public Vector2 NodeSize = new Vector2(100, 40);
        public int OffsetX = 150;
        public int OffsetY = 50;
        public int Index_X;
        public int Index_Y;
        
    }

    // -------------------- Unity --------------------
    [MenuItem("ET/View/Scene层级关系视图")]
    static void OpenWindow() => GetWindow<ETSceneNodeGraph>("ET Node Graph");
    
    void OnEnable()
    {
        textureCache ??= new Dictionary<Color, Texture2D>();

        if (FiberManager.Instance == null)
        {
            return;
        }
        var fibers = FiberManager.Instance.GetFibers();
        foreach (var fiber in fibers)
        {
            NodeData nodeData = new NodeData();
            var nodeName = $"{fiber.Value.Root.GetType().Name}-{fiber.Value.Root.Id}-{fiber.Value.Root.Id}-{fiber.Value.Root.InstanceId}"; 
            if(maxIndexX != 0)
            {
                nodeData.Index_X = maxIndexX;
                nodeData.StartPos = startPos + new Vector2(nodeData.OffsetX * nodeData.Index_X, nodeData.OffsetY * nodeData.Index_Y);
            }
            ETNode start = new(nodeData.StartPos, nodeData.NodeSize, nodeName, Color.green);
            nodeData.NodeList.Add(start);
            nodeData.StartNode = start;

            nodeData.Index_X++;
            if (fiber.Value.Root.Components != null)
            {
                FiberComponent(fiber.Value.Root.Components,nodeData);
            }

            if (fiber.Value.Root.Children != null)
            {
                FiberChildComponent(fiber.Value.Root.Children,nodeData);
            }
            
            startPos = new Vector2(nodeData.StartPos.x, startPos.y);
            nodeDataList.Add(nodeData);
        }
    }

    private void FiberComponent(SortedDictionary<long,Entity> components, NodeData nodeData)
    {
        if (components is { Count: > 0 })
        {
            foreach (var component in components)
            {
                var nodeName = $"{component.Value.GetType().Name} - {component.Value.Id} - {component.Value.InstanceId}";
                nodeData.StartPos = startPos + new Vector2(nodeData.OffsetX * nodeData.Index_X, nodeData.OffsetY * nodeData.Index_Y);
                ETNode etNode = new(nodeData.StartPos, nodeData.NodeSize, nodeName, Color.green);
                nodeData.NodeList.Add(etNode);
                nodeData.ConnectionList.Add(new Connection(nodeData.StartNode, etNode));

                // 备份当前上下文
                ETNode prevStartNode = nodeData.StartNode;
                int prevIndexX = nodeData.Index_X;
                int prevIndexY = nodeData.Index_Y;

                // 如果有 Components（相当于同类深度），以当前节点为父节点，X 向右一列，Y 从 prevIndexY 开始
                if (component.Value.Components != null && component.Value.Components.Count > 0)
                {
                    nodeData.StartNode = etNode;
                    nodeData.Index_X = prevIndexX + 1;
                    nodeData.Index_Y = prevIndexY;
                    FiberComponent(component.Value.Components, nodeData);
                    // 确保递归后 StartNode 恢复为当前节点，以便 Children 能正确连接
                    nodeData.StartNode = etNode;
                }

                // 如果有 Children，也以当前节点为父节点，X 向右一列，Y 从 prevIndexY 开始
                if (component.Value.Children != null && component.Value.Children.Count > 0)
                {
                    nodeData.StartNode = etNode;
                    nodeData.Index_X = prevIndexX + 1;
                    nodeData.Index_Y = prevIndexY;
                    FiberChildComponent(component.Value.Children, nodeData);
                    // 确保递归后 StartNode 恢复为当前节点
                    nodeData.StartNode = etNode;
                }

                // 恢复上下文，并把 Y 增 1 作为下一个兄弟节点的起始行
                nodeData.StartNode = prevStartNode;
                nodeData.Index_X = prevIndexX;
                nodeData.Index_Y = prevIndexY + 1;
                maxIndexX = math.max(nodeData.Index_X, maxIndexX);
            }
        }
    }
    private void FiberChildComponent(SortedDictionary<long,Entity> components,NodeData nodeData)
    {
        if(components is { Count: > 0 })
        {
            foreach (var component in components)
            {
                var nodeName = $"{component.Value.GetType().Name}-Child-{component.Value.Id} - {component.Value.InstanceId}";
                nodeData.StartPos = startPos + new Vector2(nodeData.OffsetX * nodeData.Index_X, nodeData.OffsetY * nodeData.Index_Y);
                ETNode etNode = new(nodeData.StartPos,nodeData.NodeSize, nodeName, Color.green);
                nodeData.NodeList.Add(etNode);
                nodeData.ConnectionList.Add(new Connection(nodeData.StartNode, etNode));

                // 备份当前上下文
                ETNode prevStartNode = nodeData.StartNode;
                int prevIndexX = nodeData.Index_X;
                int prevIndexY = nodeData.Index_Y;

                if (component.Value.Components != null && component.Value.Components.Count > 0)
                {
                    nodeData.StartNode = etNode;
                    nodeData.Index_X = prevIndexX + 1;
                    nodeData.Index_Y = prevIndexY;
                    FiberComponent(component.Value.Components, nodeData);
                    // 确保递归后 StartNode 恢复为当前节点，以便 Children 能正确连接
                    nodeData.StartNode = etNode;
                }

                if (component.Value.Children != null && component.Value.Children.Count > 0)
                {
                    nodeData.StartNode = etNode;
                    nodeData.Index_X = prevIndexX + 1;
                    nodeData.Index_Y = prevIndexY;
                    FiberChildComponent(component.Value.Children, nodeData);
                    // 确保递归后 StartNode 恢复为当前节点
                    nodeData.StartNode = etNode;
                }

                // 恢复并为下一个兄弟节点准备行号
                nodeData.StartNode = prevStartNode;
                nodeData.Index_X = prevIndexX;
                nodeData.Index_Y = prevIndexY + 1;
            }
        }
    }
    void OnGUI()
    {
        EnsureInitialized();
        HandleMouseEvents();

        // ------------------- 绘制背景网格 -------------------
        DrawGrid(position.width, position.height, 20f, 0.2f, Color.gray);
        DrawGrid(position.width, position.height, 100f, 0.4f, Color.gray);

        foreach (var nodeData in nodeDataList)
        {
            // ------------------- 绘制节点 -------------------
            foreach (var node in nodeData.NodeList)
            {
                Rect screenRect = ToScreenRect(node.rect);
                node.Draw(nodeStyle, GetTextureForColor(node.color), screenRect);
            }

            // ------------------- 绘制连接线 -------------------
            foreach (var conn in nodeData.ConnectionList)
            {
                DrawConnection(conn);
            }
        }

        // ------------------- 显示状态 -------------------
        GUI.Label(new Rect(10, 10, 400, 20), $"偏移: {viewOffset} | 缩放: {zoomLevel:F2}x");
    }

    // -------------------- 坐标转换 --------------------
    Rect ToScreenRect(Rect logicalRect)
    {
        return new Rect(
            logicalRect.position * zoomLevel + viewOffset,
            logicalRect.size * zoomLevel
        );
    }

    Vector2 ToScreenPoint(Vector2 logicalPos) => logicalPos * zoomLevel + viewOffset;

    // -------------------- 连接线绘制 --------------------
    void DrawConnection(Connection conn)
    {
        Vector2 start = ToScreenPoint(conn.from.GetRightCenter());
        Vector2 end = ToScreenPoint(conn.to.GetLeftCenter());
        float distance = Mathf.Abs(end.x - start.x) * 0.5f;
        Vector3 startTangent = start + Vector2.right * distance;
        Vector3 endTangent = end + Vector2.left * distance;
        Handles.DrawBezier(start, end, startTangent, endTangent, Color.cyan, null, 2f);
    }
    // -------------------- 鼠标事件 --------------------
    void HandleMouseEvents()
    {
        Event e = Event.current;
        switch (e.type)
        {
            case EventType.MouseDown when e.button == 0:
                isDragging = true;
                e.Use();
                break;

            case EventType.MouseDrag when isDragging && e.button == 0:
                viewOffset += e.delta;
                Repaint(); // 拖拽实时刷新
                e.Use();
                break;

            case EventType.MouseUp when e.button == 0:
                isDragging = false;
                e.Use();
                break;

            case EventType.ScrollWheel:
                float zoomChange = -e.delta.y / 100f;
                float oldZoom = zoomLevel;
                zoomLevel = Mathf.Clamp(zoomLevel + zoomChange, MinZoom, MaxZoom);

                Vector2 mousePos = e.mousePosition;
                viewOffset = (viewOffset - mousePos) * (zoomLevel / oldZoom) + mousePos;

                Repaint();
                e.Use();
                break;

            /*case EventType.ContextClick:
                ShowContextMenu(e.mousePosition);
                e.Use();
                break;*/
        }
    }

    /*void ShowContextMenu(Vector2 mousePos)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("添加节点"), false, () =>
        {
            Vector2 worldPos = (mousePos - viewOffset) / zoomLevel;
            nodes.Add(new ETNode(worldPos, nodeSize, "新节点", Color.cyan));
            Repaint();
        });
        menu.ShowAsContext();
    }*/

    // -------------------- 网格绘制 --------------------
    void DrawGrid(float width, float height, float spacing, float opacity, Color color)
    {
        Handles.BeginGUI();
        Color prev = Handles.color;
        Handles.color = new Color(color.r, color.g, color.b, opacity);

        float startX = -viewOffset.x % (spacing * zoomLevel);
        float startY = -viewOffset.y % (spacing * zoomLevel);

        for (float x = startX; x < width; x += spacing * zoomLevel)
            Handles.DrawLine(new Vector3(x, 0, 0), new Vector3(x, height, 0));

        for (float y = startY; y < height; y += spacing * zoomLevel)
            Handles.DrawLine(new Vector3(0, y, 0), new Vector3(width, y, 0));

        Handles.color = prev;
        Handles.EndGUI();
    }

    // -------------------- 工具方法 --------------------
    void EnsureInitialized()
    {
        if (nodeStyle == null)
        {
            GUIStyle baseStyle = GUI.skin != null ? GUI.skin.box : new GUIStyle();
            nodeStyle = new GUIStyle(baseStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }
    }

    Texture2D GetTextureForColor(Color color)
    {
        if (!textureCache.TryGetValue(color, out var tex) || tex == null)
        {
            tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            textureCache[color] = tex;
        }
        return tex;
    }
}