using System;

    //=========================================================================
public interface IUIRequestOption
{

}

    //=========================================================================
public struct UIRequestGroup : IUIRequestOption
{
    //---------------------------------------------------------------------
    public string name;

    //---------------------------------------------------------------------
    public UIRequestGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            this.name = "DefaultGroup";
        }
        else
        {
            this.name = groupName;
        }
    }

    //---------------------------------------------------------------------
    public static UIRequestGroup Unique = new UIRequestGroup(
        "UniqueGroup_" + DateTime.Now.Ticks.ToString());

    //---------------------------------------------------------------------
    public static UIRequestGroup Defulat = new UIRequestGroup(string.Empty);
}