using System;
using UnityEngine;

public interface IRoadEventResolver
{
    void Resolve(CRoadEventData roadEvent, CRunContext ctx, Action onDone);
}

public class CAutoRoadEventResolver : IRoadEventResolver
{
    public void Resolve(CRoadEventData roadEvent, CRunContext ctx, Action onDone)
    {
        if (roadEvent == null || roadEvent.Options == null || roadEvent.Options.Count == 0)
        {
            onDone?.Invoke();
            return;
        }

        CRoadEventOption option = roadEvent.Options[0];

        CPrint.Log($"이벤트 {roadEvent.Id} ({roadEvent.Category}) → {option.LabelKey}");

        for (int i = 0; i < option.Effects.Count; i++)
        {
            if (option.Effects[i] == null)
            {
                continue;
            }

            option.Effects[i].Apply(ctx);
        }

        onDone?.Invoke();
    }
}
