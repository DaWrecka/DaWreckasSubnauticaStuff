using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Utility
{
	public static class LanguageUtils
	{
		public static string Get(TechType tt)
		{
			try
			{
				return Language.main.Get(tt);
			}
			catch (Exception e)
			{
				Log.LogError($"LanguageUtils.Get(): Exception caught trying to retrieve text for TechType {tt.AsString()}:\n{e.ToString()}");
				return String.Empty;
			}
		}
	}
}
