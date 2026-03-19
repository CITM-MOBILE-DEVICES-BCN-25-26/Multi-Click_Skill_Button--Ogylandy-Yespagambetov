using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class MultiClickSkillButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float longPressThreshold = 0.5f;
    [SerializeField] private float doubleClickThreshold = 0.3f;

    private const int FIRST_CLICK_COUNT = 1;
    private const int SECOND_CLICK_COUNT = 2;

    private SkillButtonState currentState = SkillButtonState.Idle;
    private CancellationTokenSource sequenceCancellationTokenSource;
    private bool isPointerDown;
    private bool actionTriggered;
    private int clickCount;

    private enum SkillButtonState
    {
        Idle,
        PressingFirstClick,
        WaitingForSecondClick,
        PressingSecondClick
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (actionTriggered)
        {
            return;
        }

        if (currentState == SkillButtonState.Idle)
        {
            StartNewSequence();
            isPointerDown = true;
            currentState = SkillButtonState.PressingFirstClick;
            _ = MonitorLongPressAsync(sequenceCancellationTokenSource.Token);
            return;
        }

        if (currentState == SkillButtonState.WaitingForSecondClick)
        {
            isPointerDown = true;
            currentState = SkillButtonState.PressingSecondClick;
            _ = MonitorLongPressAsync(sequenceCancellationTokenSource.Token);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (actionTriggered)
        {
            return;
        }

        isPointerDown = false;

        if (currentState == SkillButtonState.PressingFirstClick)
        {
            clickCount = FIRST_CLICK_COUNT;
            currentState = SkillButtonState.WaitingForSecondClick;
            _ = WaitForSecondClickAsync(sequenceCancellationTokenSource.Token);
            return;
        }

        if (currentState == SkillButtonState.PressingSecondClick)
        {
            clickCount = SECOND_CLICK_COUNT;
            TriggerDoubleClick();
        }
    }

    private void StartNewSequence()
    {
        CancelCurrentSequence();

        sequenceCancellationTokenSource = new CancellationTokenSource();
        actionTriggered = false;
        clickCount = 0;
    }

    private async Task MonitorLongPressAsync(CancellationToken cancellationToken)
    {
        try
        {
            int delayMilliseconds = Mathf.RoundToInt(longPressThreshold * 1000.0f);
            await Task.Delay(delayMilliseconds, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!isPointerDown)
            {
                return;
            }

            TriggerLongPress();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task WaitForSecondClickAsync(CancellationToken cancellationToken)
    {
        try
        {
            int delayMilliseconds = Mathf.RoundToInt(doubleClickThreshold * 1000.0f);
            await Task.Delay(delayMilliseconds, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (currentState != SkillButtonState.WaitingForSecondClick)
            {
                return;
            }

            if (clickCount != FIRST_CLICK_COUNT)
            {
                return;
            }

            TriggerShortClick();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void TriggerShortClick()
    {
        if (actionTriggered)
        {
            return;
        }

        actionTriggered = true;
        Debug.Log("Short Click -> Action A");
        ResetToIdle();
    }

    private void TriggerLongPress()
    {
        if (actionTriggered)
        {
            return;
        }

        actionTriggered = true;
        Debug.Log("Long Press -> Action B");
        ResetToIdle();
    }

    private void TriggerDoubleClick()
    {
        if (actionTriggered)
        {
            return;
        }

        actionTriggered = true;
        Debug.Log("Double Click -> Action C");
        ResetToIdle();
    }

    private void ResetToIdle()
    {
        CancelCurrentSequence();
        currentState = SkillButtonState.Idle;
        clickCount = 0;
        isPointerDown = false;
    }

    private void CancelCurrentSequence()
    {
        if (sequenceCancellationTokenSource == null)
        {
            return;
        }

        sequenceCancellationTokenSource.Cancel();
        sequenceCancellationTokenSource.Dispose();
        sequenceCancellationTokenSource = null;
    }

    private void OnDisable()
    {
        ResetToIdle();
    }

    private void OnDestroy()
    {
        ResetToIdle();
    }
}