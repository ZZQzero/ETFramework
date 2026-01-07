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

        // AttackConfig 全局参数（TrackInfoPanel 下方）
        inputBufferWindowMsField = rightContainer.Q<IntegerField>("InputBufferWindowMsField");
        defaultHitStopMsField = rightContainer.Q<IntegerField>("DefaultHitStopMsField");
        recoveryHoldMsField = rightContainer.Q<IntegerField>("RecoveryHoldMsField");

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
        comboTimeoutOffsetMsField = rightContainer.Q<IntegerField>("ComboTimeoutOffsetMsField");
        segmentTimeoutMsPreviewField = rightContainer.Q<IntegerField>("SegmentTimeoutMsPreviewField");

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

        // HitEffectData / HitFeedbackData
        hitEffectDamageMultiplierField = rightContainer.Q<FloatField>("HitEffectDamageMultiplierField");
        hitEffectReactionField = rightContainer.Q<EnumField>("HitEffectReactionField");
        if (hitEffectReactionField != null)
        {
            hitEffectReactionField.Init(HitReactionType.Light);
        }
        hitEffectKnockbackForceField = rightContainer.Q<FloatField>("HitEffectKnockbackForceField");
        hitEffectKnockupForceField = rightContainer.Q<FloatField>("HitEffectKnockupForceField");
        hitEffectHitStunMsField = rightContainer.Q<IntegerField>("HitEffectHitStunMsField");
        hitEffectTargetStateField = rightContainer.Q<EnumField>("HitEffectTargetStateField");
        if (hitEffectTargetStateField != null)
        {
            hitEffectTargetStateField.Init(TargetStateType.Any);
        }

        hitFeedbackShakeIntensityField = rightContainer.Q<FloatField>("HitFeedbackShakeIntensityField");
        hitFeedbackShakeDurationField = rightContainer.Q<FloatField>("HitFeedbackShakeDurationField");
        hitFeedbackHitStopMsField = rightContainer.Q<IntegerField>("HitFeedbackHitStopMsField");
        hitFeedbackTimeScaleField = rightContainer.Q<FloatField>("HitFeedbackTimeScaleField");
        hitFeedbackTimeScaleDurationMsField = rightContainer.Q<IntegerField>("HitFeedbackTimeScaleDurationMsField");

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
        if (comboTimeoutOffsetMsField != null)
        {
            comboTimeoutOffsetMsField.tooltip = "本段连击超时偏移(ms)：本段超时 = 本段时长(ms) + 偏移。用于避免动画未播完就因超时退出。";
            comboTimeoutOffsetMsField.RegisterValueChangedCallback(OnComboTimeoutOffsetMsChanged);
        }
        if (segmentTimeoutMsPreviewField != null)
        {
            segmentTimeoutMsPreviewField.SetEnabled(false);
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

        // AttackConfig 全局参数回调（不依赖 clip 选择）
        if (inputBufferWindowMsField != null)
        {
            inputBufferWindowMsField.tooltip = "输入缓冲有效期(ms)：缓存输入在被消费前能保留多久；与动画窗口(0-1)的 InputBufferStart 不同。";
            inputBufferWindowMsField.RegisterValueChangedCallback(OnInputBufferWindowMsChanged);
        }
        if (defaultHitStopMsField != null)
        {
            defaultHitStopMsField.tooltip = "默认顿帧(ms)：当 HitFeedback.HitStopMs <= 0 时回退使用该值。";
            defaultHitStopMsField.RegisterValueChangedCallback(OnDefaultHitStopMsChanged);
        }
        if (recoveryHoldMsField != null)
        {
            recoveryHoldMsField.tooltip = "后摇保持(ms)：进入Recovery后保持AttackLayer的时间，超时后淡出回到Move/Idle。";
            recoveryHoldMsField.RegisterValueChangedCallback(OnRecoveryHoldMsChanged);
        }

        // HitEffectData / HitFeedbackData 回调（跟随 HitBox clip）
        if (hitEffectDamageMultiplierField != null)
        {
            hitEffectDamageMultiplierField.tooltip = "伤害倍率：最终伤害 = 基础伤害 × 倍率。";
            hitEffectDamageMultiplierField.RegisterValueChangedCallback(OnHitEffectDamageMultiplierChanged);
        }
        if (hitEffectReactionField != null)
        {
            hitEffectReactionField.tooltip = "受击反应类型：决定目标播放哪种受击/击退/击飞。";
            hitEffectReactionField.RegisterValueChangedCallback(OnHitEffectReactionChanged);
        }
        if (hitEffectKnockbackForceField != null)
        {
            hitEffectKnockbackForceField.tooltip = "击退力度：用于击退/击倒类反应。";
            hitEffectKnockbackForceField.RegisterValueChangedCallback(OnHitEffectKnockbackForceChanged);
        }
        if (hitEffectKnockupForceField != null)
        {
            hitEffectKnockupForceField.tooltip = "击飞力度：用于击飞类反应。";
            hitEffectKnockupForceField.RegisterValueChangedCallback(OnHitEffectKnockupForceChanged);
        }
        if (hitEffectHitStunMsField != null)
        {
            hitEffectHitStunMsField.tooltip = "硬直时间(ms)：目标受击后无法行动的持续时间。";
            hitEffectHitStunMsField.RegisterValueChangedCallback(OnHitEffectHitStunMsChanged);
        }
        if (hitEffectTargetStateField != null)
        {
            hitEffectTargetStateField.tooltip = "目标状态过滤：用于限制该 HitBox 只命中某些状态目标。";
            hitEffectTargetStateField.RegisterValueChangedCallback(OnHitEffectTargetStateChanged);
        }

        if (hitFeedbackShakeIntensityField != null)
        {
            hitFeedbackShakeIntensityField.tooltip = "震屏强度(0-1)。";
            hitFeedbackShakeIntensityField.RegisterValueChangedCallback(OnHitFeedbackShakeIntensityChanged);
        }
        if (hitFeedbackShakeDurationField != null)
        {
            hitFeedbackShakeDurationField.tooltip = "震屏持续时间(秒)。";
            hitFeedbackShakeDurationField.RegisterValueChangedCallback(OnHitFeedbackShakeDurationChanged);
        }
        if (hitFeedbackHitStopMsField != null)
        {
            hitFeedbackHitStopMsField.tooltip = "顿帧时长(ms)：<=0 表示不覆盖，回退使用 DefaultHitStopMs。";
            hitFeedbackHitStopMsField.RegisterValueChangedCallback(OnHitFeedbackHitStopMsChanged);
        }
        if (hitFeedbackTimeScaleField != null)
        {
            hitFeedbackTimeScaleField.tooltip = "时间缩放：1 为正常，小于 1 为慢动作（配合持续时间使用）。";
            hitFeedbackTimeScaleField.RegisterValueChangedCallback(OnHitFeedbackTimeScaleChanged);
        }
        if (hitFeedbackTimeScaleDurationMsField != null)
        {
            hitFeedbackTimeScaleDurationMsField.tooltip = "时间缩放持续时间(ms)。";
            hitFeedbackTimeScaleDurationMsField.RegisterValueChangedCallback(OnHitFeedbackTimeScaleDurationMsChanged);
        }

        // 初始状态：隐藏所有字段组
        HideAllClipFields();

        // 初始状态：没有选中任何轨道或Clip
        ClearSelection();
    }

    #endregion
}

