namespace SCP_575
{
	public static class Extensions
	{
		public static bool HasLightSource(this ReferenceHub rh)
		{
			if (rh.inventory != null && rh.inventory.curItem == ItemType.Flashlight)
            {
				return true;
			}
            else
            {

            }
		    return true;;
		}
	}
}