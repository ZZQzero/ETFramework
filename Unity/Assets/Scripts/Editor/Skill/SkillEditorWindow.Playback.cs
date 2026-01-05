using System;
using System.Collections.Generic;
using Animancer;
using ET;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public partial class SkillEditorWindow : EditorWindow
{
    #region 播放控制按钮事件

    /// <summary>
    /// Play按钮点击事件
    /// </summary>
    private void OnPlayButtonClicked()
    {
        // TODO: 实现播放功能
        // 1. 设置 isPlaying = true
        // 2. 如果 animancer 不为空，开始播放动画
        // 3. 根据 currentPlaybackTime 找到对应的 segment
        // 4. 播放对应的动画片段，设置正确的起始时间（normalizedTime）
        // 5. 设置动画播放速度为 playbackSpeed
        // 6. 如果是循环模式（isLooping），设置动画循环
        // 7. 可能需要启动一个协程或Update方法来更新 currentPlaybackTime
        // 8. 更新按钮状态（如果需要显示暂停/播放图标）
        
        isPlaying = true;
        Debug.Log($"Play clicked. Current time: {currentPlaybackTime:F2}s, Speed: {playbackSpeed:F1}x, Loop: {isLooping}");
    }

    /// <summary>
    /// Stop按钮点击事件
    /// </summary>
    private void OnStopButtonClicked()
    {
        // TODO: 实现停止功能
        // 1. 设置 isPlaying = false
        // 2. 如果 animancer 不为空，停止所有动画播放
        // 3. 重置 currentPlaybackTime = 0
        // 4. 更新播放进度条位置（调用 UpdatePlayheadPosition）
        // 5. 更新动画预览（调用 UpdateAnimationPreview）
        // 6. 更新按钮状态
        
        isPlaying = false;
        currentPlaybackTime = 0f;
        UpdatePlayheadPosition();
        UpdateAnimationPreview();
        Debug.Log("Stop clicked. Reset playback time to 0.");
    }

    /// <summary>
    /// Loop按钮点击事件
    /// </summary>
    private void OnLoopButtonClicked()
    {
        // TODO: 实现循环切换功能
        // 1. 切换 isLooping 状态（true <-> false）
        // 2. 更新按钮视觉状态（例如改变按钮文字或样式，显示"Loop: ON/OFF"）
        // 3. 如果正在播放，更新当前动画的循环设置
        // 4. 如果使用 Animancer，设置 AnimancerState.IsLooping
        
        isLooping = !isLooping;
        
        // 更新按钮文字显示循环状态
        var loopBtn = root.Q<Button>("Loop");
        if (loopBtn != null)
        {
            loopBtn.text = isLooping ? "Loop: ON" : "Loop: OFF";
        }
        
        // 如果正在播放，更新动画循环设置
        if (isPlaying && animancer != null)
        {
            // TODO: 更新当前播放动画的循环设置
            // if (animancer.CurrentState != null)
            // {
            //     animancer.CurrentState.IsLooping = isLooping;
            // }
        }
        
        Debug.Log($"Loop clicked. Loop mode: {isLooping}");
    }
    #endregion

    // 初始化播放速度控制（Top Bar）
    private void InitPlaybackSpeedControl()
    {
        // 找到Speed标签后面的Slider（在Top容器中查找）
        var topContainer = root.Q<VisualElement>("Top");
        if (topContainer != null)
        {
            // 查找Slider（在Speed标签后面）
            var speedLabel = root.Q<Label>("Speed");
            if (speedLabel != null)
            {
                // 查找Speed标签后面的Slider
                var elements = topContainer.Children();
                bool foundSpeed = false;
                foreach (var element in elements)
                {
                    if (element == speedLabel)
                    {
                        foundSpeed = true;
                        continue;
                    }
                    if (foundSpeed && element is Slider slider)
                    {
                        speedSlider = slider;
                        break;
                    }
                }
            }

            // 获取SpeedNum（TextField，可直接编辑）
            speedNumField = root.Q<TextField>("SpeedNum");
        }

        // 设置Slider范围（0-6）
        if (speedSlider != null)
        {
            speedSlider.lowValue = 0f;
            speedSlider.highValue = 6f;
            speedSlider.value = playbackSpeed; // 初始值1.0

            // 监听Slider值变化
            speedSlider.RegisterValueChangedCallback(evt =>
            {
                playbackSpeed = evt.newValue;
                UpdateSpeedDisplay();
            });
        }

        // 监听SpeedNum值变化（用户直接编辑时）
        if (speedNumField != null)
        {
            speedNumField.RegisterValueChangedCallback(evt =>
            {
                string input = evt.newValue.Trim();

                // 移除"x"后缀（如果有）
                if (input.EndsWith("x", StringComparison.OrdinalIgnoreCase))
                {
                    input = input.Substring(0, input.Length - 1);
                }

                if (float.TryParse(input, out float speedValue))
                {
                    speedValue = Mathf.Clamp(speedValue, 0f, 6f);
                    playbackSpeed = speedValue;

                    // 更新Slider位置（使用SetValueWithoutNotify避免触发回调）
                    if (speedSlider != null)
                    {
                        speedSlider.SetValueWithoutNotify(playbackSpeed);
                    }

                    UpdateSpeedDisplay();
                }
                else
                {
                    UpdateSpeedDisplay();
                }
            });
        }

        // 初始化显示
        UpdateSpeedDisplay();
    }

    // 更新速度显示
    private void UpdateSpeedDisplay()
    {
        if (speedNumField == null) return;

        if (Mathf.Approximately(playbackSpeed, Mathf.Round(playbackSpeed)))
        {
            speedNumField.value = $"{(int)playbackSpeed}x";
        }
        else
        {
            speedNumField.value = $"{playbackSpeed:F1}x";
        }
    }
}