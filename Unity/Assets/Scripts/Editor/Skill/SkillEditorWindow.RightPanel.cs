using ET;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public partial class SkillEditorWindow : EditorWindow
{
    #region RightPanel - 初始化（查询UXML + 注册回调）

    private static void SetVector3FieldTwoDecimals(Vector3Field field)
    {
        if (field == null)
        {
            return;
        }

        // Vector3Field 内部由 3 个 FloatField 组成，统一把显示格式改为两位小数
        field.Query<FloatField>().ForEach(f => f.formatString = "0.00");
    }

    // 初始化Right面板UI元素
    private void InitRightPanel()
    {
        // 先获取Right容器
        var rightContainer = root.Q<VisualElement>("Right");
        if (rightContainer == null)
        {
            Debug.LogWarning("Right容器未找到！");
            return;
        }

        // 获取轨道信息面板的UI元素
        trackTypeLabel = rightContainer.Q<Label>("TrackTypeLabel");
        clipCountLabel = rightContainer.Q<Label>("ClipCountLabel");
        totalDurationLabel = rightContainer.Q<Label>("TotalDurationLabel");

        // 获取Clip属性面板的标题和字段组
        clipPropertiesTitle = rightContainer.Q<Label>("ClipPropertiesTitle");
        animationFields = rightContainer.Q<VisualElement>("AnimationFields");
        effectFields = rightContainer.Q<VisualElement>("EffectFields");
        soundFields = rightContainer.Q<VisualElement>("SoundFields");
        hitBoxFields = rightContainer.Q<VisualElement>("HitBoxFields");

        // 获取通用字段
        clipNameField = rightContainer.Q<TextField>("ClipNameField");
        startTimeField = rightContainer.Q<FloatField>("StartTimeField");
        frameField = rightContainer.Q<IntegerField>("FrameField");
        clipLengthField = rightContainer.Q<FloatField>("ClipLengthField");
        clipIndexLabel = rightContainer.Q<Label>("ClipIndexLabel");

        // 获取Animation Clip字段
        animationActionButtons = rightContainer.Q<VisualElement>("AnimationActionButtons");
        addEffectButton = rightContainer.Q<Button>("AddEffectButton");
        addSoundButton = rightContainer.Q<Button>("AddSoundButton");
        addHitboxButton = rightContainer.Q<Button>("AddHitboxButton");
        animationClipField = rightContainer.Q<ObjectField>("AnimationClipField");
        if (animationClipField != null)
        {
            animationClipField.objectType = typeof(UnityEngine.AnimationClip);
        }
        speedField = rightContainer.Q<FloatField>("SpeedField");
        durationField = rightContainer.Q<FloatField>("DurationField");
        fadeDurationField = rightContainer.Q<FloatField>("FadeDurationField");
        inputBufferStartField = rightContainer.Q<FloatField>("InputBufferStartField");
        cancelableTimeField = rightContainer.Q<FloatField>("CancelableTimeField");
        animationEndField = rightContainer.Q<FloatField>("AnimationEndField");

        // 获取Effect Clip字段
        effectPrefabField = rightContainer.Q<ObjectField>("EffectPrefabField");
        if (effectPrefabField != null)
        {
            effectPrefabField.objectType = typeof(UnityEngine.GameObject);
        }
        effectTriggerTimeField = rightContainer.Q<FloatField>("EffectTriggerTimeField");
        effectNormalizedStartField = rightContainer.Q<FloatField>("EffectNormalizedStartField");
        followTargetField = rightContainer.Q<Toggle>("FollowTargetField");

        // 获取Sound Clip字段
        audioClipField = rightContainer.Q<ObjectField>("AudioClipField");
        if (audioClipField != null)
        {
            audioClipField.objectType = typeof(UnityEngine.AudioClip);
        }
        soundTriggerTimeField = rightContainer.Q<FloatField>("SoundTriggerTimeField");
        soundNormalizedStartField = rightContainer.Q<FloatField>("SoundNormalizedStartField");
        volumeField = rightContainer.Q<FloatField>("VolumeField");

        // 获取HitBox Clip字段
        shapeTypeField = rightContainer.Q<EnumField>("ShapeTypeField");
        if (shapeTypeField != null)
        {
            shapeTypeField.Init(HitShapeType.Box);
        }
        hitBoxTriggerTimeField = rightContainer.Q<FloatField>("HitBoxTriggerTimeField");
        hitBoxNormalizedStartField = rightContainer.Q<FloatField>("HitBoxNormalizedStartField");
        hitBoxNormalizedEndField = rightContainer.Q<FloatField>("HitBoxNormalizedEndField");
        hitBoxOffsetField = rightContainer.Q<Vector3Field>("HitBoxOffsetField");
        hitBoxRotationField = rightContainer.Q<Vector3Field>("HitBoxRotationField");
        hitBoxSizeField = rightContainer.Q<Vector3Field>("HitBoxSizeField");

        // 右侧面板 Vector3 输入显示两位小数（避免小数位过多导致显示不下）
        SetVector3FieldTwoDecimals(hitBoxOffsetField);
        SetVector3FieldTwoDecimals(hitBoxRotationField);
        SetVector3FieldTwoDecimals(hitBoxSizeField);

        // 获取操作按钮（使用root查找确保能找到嵌套的元素）
        addClipToTrackButton = root.Q<Button>("AddClipToTrackButton");
        deleteClipButton = root.Q<Button>("DeleteClipButton");

        // 设置添加动画片段按钮的永久样式
        if (addClipToTrackButton != null)
        {
            addClipToTrackButton.AddToClassList("track-add-button");
            addClipToTrackButton.clicked += OnAddClipToTrackButtonClicked;
        }
        if (deleteClipButton != null)
        {
            deleteClipButton.clicked += OnDeleteClipButtonClicked;
        }

        // 注册所有字段的值变化事件
        if (clipNameField != null)
        {
            clipNameField.RegisterValueChangedCallback(OnClipNameChanged);
        }
        if (startTimeField != null)
        {
            startTimeField.RegisterValueChangedCallback(OnStartTimeChanged);
        }
        if (clipLengthField != null)
        {
            clipLengthField.RegisterValueChangedCallback(OnClipLengthChanged);
        }
        // Frame字段不注册回调，因为AnimationClip的帧数是只读的，由动画时长自动计算
        if (addEffectButton != null)
        {
            addEffectButton.clicked += OnAddEffectButtonClicked;
        }
        if (addSoundButton != null)
        {
            addSoundButton.clicked += OnAddSoundButtonClicked;
        }
        if (addHitboxButton != null)
        {
            addHitboxButton.clicked += OnAddHitboxButtonClicked;
        }
        if (animationClipField != null)
        {
            animationClipField.RegisterValueChangedCallback(OnAnimationClipChanged);
        }
        if (speedField != null)
        {
            speedField.RegisterValueChangedCallback(OnSpeedChanged);
        }
        if (fadeDurationField != null)
        {
            fadeDurationField.RegisterValueChangedCallback(OnFadeDurationChanged);
        }
        if (inputBufferStartField != null)
        {
            inputBufferStartField.RegisterValueChangedCallback(OnInputBufferStartChanged);
        }
        if (cancelableTimeField != null)
        {
            cancelableTimeField.RegisterValueChangedCallback(OnCancelableTimeChanged);
        }
        if (animationEndField != null)
        {
            animationEndField.RegisterValueChangedCallback(OnAnimationEndChanged);
        }
        if (effectPrefabField != null)
        {
            effectPrefabField.RegisterValueChangedCallback(OnEffectPrefabChanged);
        }
        if (effectTriggerTimeField != null)
        {
            // 触发时间只读显示
            effectTriggerTimeField.SetEnabled(false);
        }
        if (effectNormalizedStartField != null)
        {
            effectNormalizedStartField.RegisterValueChangedCallback(OnEffectNormalizedStartChanged);
        }
        if (followTargetField != null)
        {
            followTargetField.RegisterValueChangedCallback(OnFollowTargetChanged);
        }
        if (audioClipField != null)
        {
            audioClipField.RegisterValueChangedCallback(OnAudioClipChanged);
        }
        if (soundTriggerTimeField != null)
        {
            // 触发时间只读显示
            soundTriggerTimeField.SetEnabled(false);
        }
        if (soundNormalizedStartField != null)
        {
            soundNormalizedStartField.RegisterValueChangedCallback(OnSoundNormalizedStartChanged);
        }
        if (volumeField != null)
        {
            volumeField.RegisterValueChangedCallback(OnVolumeChanged);
        }
        if (shapeTypeField != null)
        {
            shapeTypeField.RegisterValueChangedCallback(OnShapeTypeChanged);
        }
        if (hitBoxNormalizedStartField != null)
        {
            hitBoxNormalizedStartField.RegisterValueChangedCallback(OnHitBoxNormalizedStartChanged);
        }
        if (hitBoxNormalizedEndField != null)
        {
            hitBoxNormalizedEndField.RegisterValueChangedCallback(OnHitBoxNormalizedEndChanged);
        }
        if (hitBoxOffsetField != null)
        {
            hitBoxOffsetField.RegisterValueChangedCallback(OnHitBoxOffsetChanged);
        }
        if (hitBoxRotationField != null)
        {
            hitBoxRotationField.RegisterValueChangedCallback(OnHitBoxRotationChanged);
        }
        if (hitBoxSizeField != null)
        {
            hitBoxSizeField.RegisterValueChangedCallback(OnHitBoxSizeChanged);
        }

        // 初始状态：隐藏所有字段组
        HideAllClipFields();

        // 初始状态：没有选中任何轨道或Clip
        ClearSelection();
    }

    #endregion
}

