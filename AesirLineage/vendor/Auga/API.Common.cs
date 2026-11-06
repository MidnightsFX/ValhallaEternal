// A copy of Auga's API 2.0.0 stub (Auga/API/API.Common.cs in the Auga repo), whole and unchanged below these lines.
// API makes it the stub, whose calls find Auga.dll at runtime (see Forward at the end of API.cs), and
// AUGA_API_INTERNAL keeps its types internal to this mod. Replace both files together with a newer copy.
#define API
#define AUGA_API_INTERNAL
// Its data classes are only filled by the stub, through reflection.
#pragma warning disable CS0649

using GUIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    // The types the API's signatures use. A mod compiling the stub has its own copies, which the stub converts to and
    // from Auga's (see API.Forward); AUGA_API_INTERNAL keeps them internal to the mod, as it does API.
#if AUGA_API_INTERNAL
    internal enum RequirementWireState
#else
    public enum RequirementWireState
#endif
    {
        Absent,
        Have,
        DontHave
    }

#if AUGA_API_INTERNAL
    internal class PlayerPanelTabData
#else
    public class PlayerPanelTabData
#endif
    {
        public int Index;
        public TMP_Text TabTitle;
        public GameObject TabButtonGO;
        public GameObject ContentGO;
    }

#if AUGA_API_INTERNAL
    internal class WorkbenchTabData
#else
    public class WorkbenchTabData
#endif
    {
        public int Index;
        public TMP_Text TabTitle;
        public GameObject TabButtonGO;
        public GameObject RequirementsPanelGO;
        public GameObject ItemInfoGO;
    }
#if AUGA_API_INTERNAL
    internal class CraftingControls
#else
    public class CraftingControls
#endif
    {
        public Button CraftButton;
        public GameObject Multicraft;
        public Button PlusButton;
        public Button MinusButton;
        public TMP_Text CraftAmountText;
        public GameObject CraftAmountBG;
        public GameObject Amount;
        public GuiInputField InputAmount;
        public TMP_Text InputText;
    }
}
