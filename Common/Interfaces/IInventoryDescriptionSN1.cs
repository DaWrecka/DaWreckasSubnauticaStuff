using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Interfaces
{
#if SN1
	public interface IInventoryDescriptionSN1
	{
		string GetInventoryDescription();
	}
#endif
}
