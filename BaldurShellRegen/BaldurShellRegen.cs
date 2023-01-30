using Modding;
using System;
using HutongGames.PlayMaker.Actions;
using HKMirror;
using Satchel;
using Satchel.BetterMenus;

namespace BaldurShellRegen
{
    #region Menu
    public static class ModMenu
    {
        private static Menu? MenuRef;
        public static MenuScreen CreateModMenu(MenuScreen modlistmenu)
        {
            MenuRef ??= new Menu("Baldur Shell Regen Options", new Element[]
            {
                Blueprints.HorizontalBoolOption
                (
                    "Active",
                    "Should the mod do things",
                    (b) =>
                    {
                        BaldurShellRegenMod.LS.repairs = b;
                    },
                    () => BaldurShellRegenMod.LS.repairs
                ),

                Blueprints.HorizontalBoolOption
                (
                    "Focus Charms",
                    "Should Focus Charms impact the rate",
                    (b) =>
                    {
                        BaldurShellRegenMod.LS.focusCharms = b;
                    },
                    () => BaldurShellRegenMod.LS.focusCharms
                ),

                new TextPanel
                (
                    "Heals to repair to each stage. 0 is infinite",
                    2000,
                    50
                ),

                new CustomSlider
                (
                    "Stage 4 (fully healed)",
                    f =>
                    {
                        BaldurShellRegenMod.LS.repair4 = (int)f;
                    },
                    () => BaldurShellRegenMod.LS.repair4,
                    0f,
                    10f,
                    true
                ),

                new CustomSlider
                (
                    "Stage 3",
                    f =>
                    {
                        BaldurShellRegenMod.LS.repair3 = (int)f;
                    },
                    () => BaldurShellRegenMod.LS.repair3,
                    0f,
                    10f,
                    true
                ),

                new CustomSlider
                (
                    "Stage 2",
                    f =>
                    {
                        BaldurShellRegenMod.LS.repair2 = (int)f;
                    },
                    () => BaldurShellRegenMod.LS.repair2,
                    0f,
                    10f,
                    true
                ),

                new CustomSlider
                (
                    "Stage 1",
                    f =>
                    {
                        BaldurShellRegenMod.LS.repair1 = (int)f;
                    },
                    () => BaldurShellRegenMod.LS.repair1,
                    0f,
                    10f,
                    true
                )
            });
            return MenuRef.GetMenuScreen(modlistmenu);
        }
    }
    #endregion

    public class BaldurShellRegenMod : Mod, ICustomMenuMod, ILocalSettings<LocalSettings>
    {
        #region Boilerplate
        private static BaldurShellRegenMod? _instance;
        internal static BaldurShellRegenMod Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"An instance of {nameof(BaldurShellRegenMod)} was never constructed");
                }
                return _instance;
            }
        }
        public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates) => ModMenu.CreateModMenu(modListMenu);
        public bool ToggleButtonInsideMenu => false;
        public static LocalSettings LS { get; private set; } = new();
        public void OnLoadLocal(LocalSettings s) => LS = s;
        public LocalSettings OnSaveLocal() => LS;
        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();
        public BaldurShellRegenMod() : base("BaldurShellRegen")
        {
            _instance = this;
        }
        #endregion

        #region Custom Vars
        public float heals = 0f;
        #endregion

        #region Init
        public override void Initialize()
        {
            Log("Initializing");

            On.HutongGames.PlayMaker.Actions.Wait.OnEnter += BaldurShell;
            On.HutongGames.PlayMaker.Actions.PlayerDataIntAdd.OnEnter += HealReset;

            Log("Initialized");
        }
        #endregion

        #region Changes
        private void HealReset(On.HutongGames.PlayMaker.Actions.PlayerDataIntAdd.orig_OnEnter orig, PlayerDataIntAdd self)
        {
            orig(self);

            if (self.Fsm.GameObject.name == "Blocker Shield" && self.Fsm.Name == "Control" && self.State.Name == "Blocker Hit")
            {
                heals = 0;
            }
        }

        private void BaldurShell(On.HutongGames.PlayMaker.Actions.Wait.orig_OnEnter orig, Wait self)
        {
            orig(self);

            if (PlayerDataAccess.equippedCharm_5 && self.Fsm.GameObject.name == "Knight" && self.Fsm.Name == "Spell Control" && self.State.Name.StartsWith("Focus Heal"))
            {
                int blocks = PlayerDataAccess.blockerHits;
                bool qf = PlayerDataAccess.equippedCharm_7;
                bool df = PlayerDataAccess.equippedCharm_34;
                if (blocks < 4 && LS.repairs)
                {
                    heals += LS.focusCharms ? 1f * (qf ? 0.5f : 1f) * (df ? 2f : 1f) : 1f;

                    if ((blocks == 0 && heals >= LS.repair1 && LS.repair1 != 0) ||
                        (blocks == 1 && heals >= LS.repair2 && LS.repair2 != 0) ||
                        (blocks == 2 && heals >= LS.repair3 && LS.repair3 != 0) ||
                        (blocks == 3 && heals >= LS.repair4 && LS.repair4 != 0))
                    {
                        PlayerDataAccess.blockerHits += 1;
                        heals = 0;
                        var blockerFSM = HeroController.instance.gameObject.transform.Find("Charm Effects/Blocker Shield").gameObject.LocateMyFSM("Control");
                        var blockerHUD = blockerFSM.GetAction<Tk2dPlayAnimation>("HUD 1", 0).gameObject.GameObject.Value;
                        if (PlayerDataAccess.blockerHits == 4)
                        {
                            blockerHUD.GetComponent<tk2dSpriteAnimator>().Play($"UI Appear");
                            blockerFSM.gameObject.Find("Hit Crack").SetActive(false);
                        }
                        else
                        {
                            blockerHUD.GetComponent<tk2dSpriteAnimator>().Play($"UI Break {4 - PlayerDataAccess.blockerHits}");
                            blockerFSM.gameObject.Find("Hit Crack").SetActive(true);
                        }
                        blockerFSM.gameObject.Find("Pusher").SetActive(false);
                    }
                }
            }
        }
        #endregion
    }
    #region Settings
    public class LocalSettings
    {
        public bool repairs = true;
        public bool focusCharms = false;
        public int repair1 = 0;
        public int repair2 = 0;
        public int repair3 = 0;
        public int repair4 = 0;
    }
    #endregion
}
