using System;
using System.Collections.Generic;
using Animancer;
using ET;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class TrackData
{
    public string Name;
}

public class SkillEditorWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;
    private ObjectField selectConfigAsset;
    private ObjectField selectObj;
    private ListView trackListView;
    private ListView listView;
    private VisualElement timelineRuler;
    private VisualElement root;
    
    private AttackConfig config;
    private AnimancerComponent animancer;
    
    private List<TrackData> trackDataList = new List<TrackData>();
    
    private const float RULER_HEIGHT = 30f;
    
    // 视图状态
    private float zoomLevel = 1.0f;        // 缩放级别
    private float pixelsPerSecond = 50f;   // 每秒像素数（受zoom影响）
    private float scrollX = 0f;           // 水平滚动位置
    private float totalTimelineDuration = 30f; // 总时间轴长度（秒）
    private float timelineScrollX;

    [MenuItem("ET/SkillEditorWindow")]
    public static void ShowExample()
    {
        SkillEditorWindow wnd = GetWindow<SkillEditorWindow>();
        wnd.titleContent = new GUIContent("SkillEditorWindow");
    }

    public void CreateGUI()
    {
        root = rootVisualElement;

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/Skill/SkillEditorWindow.uxml");
        visualTree.CloneTree(root);
        
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Skill/SkillEditorWindow.uss");
        root.styleSheets.Add(styleSheet);
        InitData();
        InitTimelineRuler();
        DrawTrack();
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    private void InitData()
    {
        selectConfigAsset = root.Q<ObjectField>("SelectConfig");
        selectConfigAsset.objectType = typeof(AttackConfigAsset);
        selectObj = root.Q<ObjectField>("SelectObj");
        selectObj.objectType = typeof(GameObject);
        trackListView = root.Q<ListView>("TrackListView");
        if (trackListView != null)
        {
            trackListView.itemsSource = trackDataList;
            trackListView.makeItem = TrackDataMakeItem;
            trackListView.bindItem = TrackDataBindItem;
        }
        listView = root.Q<ListView>("InfoListView");
        var addBtn = root.Q<Button>("Add");
        addBtn.clicked += OnAddClick;
    }

    private void OnAddClick()
    {
        if (selectConfigAsset != null)
        {
            var asset = selectConfigAsset.value as AttackConfigAsset;
            if (asset != null)
            {
                config = asset.Config;
            }
        }

        if (selectObj != null)
        {
            var obj = selectObj.value as GameObject;
            if (obj != null)
            {
                animancer = obj.GetComponent<AnimancerComponent>();
            }
        }
        DrawTrack();
    }

    private VisualElement TrackDataMakeItem()
    {
        var trackLabel = new Label("Track");
        /*trackLabel.style.width = 120;
        trackLabel.style.minWidth = 120;
        trackLabel.style.color = Color.white;
        trackLabel.style.unityTextAlign = TextAnchor.MiddleLeft;*/
        return trackLabel;
    }

    private void TrackDataBindItem(VisualElement element, int index)
    {
        if (element is Label item)
        {
            item.text = trackDataList[index].Name;
            item.style.height = RULER_HEIGHT;
        }
    }
    
    private void DrawTrack()
    {
        if (trackListView == null || config == null)
        {
            return;
        } 
        TrackData trackData = new TrackData();
        trackData.Name = "动画1";
        trackDataList.Add(trackData);
        trackListView.itemsSource = trackDataList;
        trackListView.RefreshItems();
        if (config.Segments.Count > 0)
        {
            
        }
    }
    
    private void InitTimelineRuler()
    {
        timelineRuler = root.Q<VisualElement>("Ruler");
        timelineRuler.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        timelineRuler.style.borderBottomWidth = 1;
        timelineRuler.style.borderBottomColor = Color.gray;
    }
    
}
