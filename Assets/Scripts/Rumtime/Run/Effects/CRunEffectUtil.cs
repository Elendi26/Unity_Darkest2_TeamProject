using System.Collections.Generic;

public static class CRunEffectUtil
{
	public static void ApplyAll(IReadOnlyList<IRunEffect> effects, CRunContext ctx)
	{
		if (effects == null)
		{
			return;
		}

		for (int i = 0; i < effects.Count; i++)
		{
			if (effects[i] == null)
			{
				continue;
			}

			effects[i].Apply(ctx);
		}
	}
}
