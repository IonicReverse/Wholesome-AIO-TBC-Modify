using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using wManager.Events;
using System.ComponentModel;
using WholesomeTBCAIO;
using WholesomeTBCAIO.Rotations;
using WholesomeTBCAIO.Settings;
using WholesomeTBCAIO.Helpers;
using WholesomeTBCAIO.Rotations.Shaman;
using WholesomeTBCAIO.Rotations.Druid;
using WholesomeTBCAIO.Rotations.Hunter;
using WholesomeTBCAIO.Rotations.Mage;
using WholesomeTBCAIO.Rotations.Paladin;
using WholesomeTBCAIO.Rotations.Priest;
using WholesomeTBCAIO.Rotations.Rogue;
using WholesomeTBCAIO.Rotations.Warlock;
using WholesomeTBCAIO.Rotations.Warrior;
using wManager.Wow.Enums;
using System.Linq;
using static WholesomeTBCAIO.Helpers.Enums;
using System.Collections.Generic;
using System;

public class Main : ICustomClass
{
    private static readonly BackgroundWorker _talentThread = new BackgroundWorker();
    private static readonly BackgroundWorker _racialsThread = new BackgroundWorker();
    private static readonly BackgroundWorker _partyThread = new BackgroundWorker();
    private Racials _racials = new Racials();

    public static string wowClass = ObjectManager.Me.WowClass.ToString();
    public static int humanReflexTime = 500;
    public static bool isLaunched;
    public static bool isPetDead = false;

    public static string version = "1.0.0"; // Must match version in Version.txt

    private IClassRotation selectedRotation;

    public float Range => RangeManager.GetRange();

    public void Initialize()
    {
        AIOTBCSettings.Load();
        AutoUpdater.CheckUpdate(version); // I will implement later if needed
        selectedRotation = ChooseRotation();
        InitLuaLogFrame();
        Logger.Log("This Wholesome AIO Rotation is Modify By Xetro -- If you need any help pls contact Xetro#8685 in Discord --");

        if (selectedRotation != null)
        {
            isLaunched = true;
            
            FightEvents.OnFightLoop += FightLoopHandler;
            FightEvents.OnFightStart += FightStartHandler;
            EventsLua.AttachEventLua("INSPECT_TALENT_READY", e => AIOParty.InspectTalentReadyHeandler());
            EventsLuaWithArgs.OnEventsLuaStringWithArgs += EventsWithArgsHandler;
            
            // Disable Talent is not needed
            //if (!TalentsManager._isRunning)
            //{
            //    _talentThread.DoWork += TalentsManager.DoTalentPulse;
            //    _talentThread.RunWorkerAsync();
            //}

            if (!_racials._isRunning && CombatSettings.UseRacialSkills)
            {
                _racialsThread.DoWork += _racials.DoRacialsPulse;
                _racialsThread.RunWorkerAsync();
            }

            if (!AIOParty._isRunning)
            {
                _partyThread.DoWork += AIOParty.DoPartyUpdatePulse;
                _partyThread.RunWorkerAsync();
            }

            selectedRotation.Initialize(selectedRotation);
        }
        else
        {
            Logger.LogError("Class not supported.");
        }
    }

    public void Dispose()
    {
        selectedRotation?.Dispose();
        isLaunched = false;

        //_talentThread.DoWork -= TalentsManager.DoTalentPulse;
        //_talentThread.Dispose();
        //TalentsManager._isRunning = false;

        if (CombatSettings.UseRacialSkills)
        {
            _racialsThread.DoWork -= _racials.DoRacialsPulse;
            _racialsThread.Dispose();
            _racials._isRunning = false;
        }

        _partyThread.DoWork -= AIOParty.DoPartyUpdatePulse;
        _partyThread.Dispose();
        AIOParty._isRunning = false;

        FightEvents.OnFightLoop -= FightLoopHandler;
        FightEvents.OnFightStart -= FightStartHandler;
        EventsLuaWithArgs.OnEventsLuaStringWithArgs -= EventsWithArgsHandler;
    }

    public void ShowConfiguration() => CombatSettings?.ShowConfiguration();

    private IClassRotation ChooseRotation()
    {
        string spec = CombatSettings.Specialization;
        Dictionary<string, Specs> mySpecDictionary = GetSpecDictionary();

        if (!mySpecDictionary.ContainsKey(CombatSettings.Specialization))
        {
            Logger.LogError($"Couldn't find spec {CombatSettings.Specialization} in the class dictionary");
            return null;
        }

        switch (mySpecDictionary[CombatSettings.Specialization])
        {
            // Shaman
            case Specs.ShamanEnhancement: return new Enhancement();
            case Specs.ShamanEnhancementParty: return new EnhancementParty();
            case Specs.ShamanElemental: return new Elemental();
            case Specs.ShamanRestoParty: return new ShamanRestoParty();
            // Druid
            case Specs.DruidFeral: return new Feral();
            case Specs.DruidFeralDPSParty: return new FeralDPSParty();
            case Specs.DruidFeralTankParty: return new FeralTankParty();
            case Specs.DruidRestorationParty: return new RestorationParty();
            // Hunter
            case Specs.HunterBeastMaster: return new BeastMastery();
            case Specs.HunterBeastMasterParty: return new BeastMasteryParty();
            // Mage
            case Specs.MageFrost: return new Frost();
            case Specs.MageFrostParty: return new FrostParty();
            case Specs.MageArcane: return new Arcane();
            case Specs.MageArcaneParty: return new ArcaneParty();
            case Specs.MageFire: return new Fire();
            case Specs.MageFireParty: return new FireParty();
            // Paladin
            case Specs.PaladinRetribution: return new Retribution();
            case Specs.PaladinHolyParty: return new PaladinHolyParty();
            case Specs.PaladinRetributionParty: return new RetributionParty();
            case Specs.PaladinProtectionParty: return new PaladinProtectionParty();
            // Priest
            case Specs.PriestShadow: return new Shadow();
            case Specs.PriestShadowParty: return new ShadowParty();
            case Specs.PriestHolyParty: return new HolyPriestParty();
            // Rogue
            case Specs.RogueCombat: return new Combat();
            case Specs.RogueCombatParty: return new RogueCombatParty();
            // Warlock
            case Specs.WarlockAffliction: return new Affliction();
            case Specs.WarlockDemonology: return new Demonology();
            case Specs.WarlockAfflictionParty: return new AfflictionParty();
            // Warrior
            case Specs.WarriorFury: return new Fury();
            case Specs.WarriorFuryParty: return new FuryParty();
            case Specs.WarriorProtectionParty: return new ProtectionWarrior();

            default: return null;
        }
    }

    private BaseSettings CombatSettings
    {
        get
        {
            switch (wowClass)
            {
                case "Shaman": return ShamanSettings.Current;
                case "Druid": return DruidSettings.Current;
                case "Hunter": return HunterSettings.Current;
                case "Mage": return MageSettings.Current;
                case "Paladin": return PaladinSettings.Current;
                case "Priest": return PriestSettings.Current;
                case "Rogue": return RogueSettings.Current;
                case "Warlock": return WarlockSettings.Current;
                case "Warrior": return WarriorSettings.Current;
                default: return null;
            }
        }
    }

    // EVENT HANDLERS
    private void InitLuaLogFrame()
    {
      var luaString = @"
				function OnEvent(self, event)
					argsForCombatLogEventPuller = { }
					eventInfo = {CombatLogGetCurrentEventInfo()}								
					for key, value in pairs(eventInfo) do
						table.insert(argsForCombatLogEventPuller, tostring(value))										
					end
				end
				f = CreateFrame('Frame')
				f:RegisterEvent('COMBAT_LOG_EVENT_UNFILTERED')
				f:SetScript('OnEvent', OnEvent)";
      Lua.LuaDoString(luaString);
    }

    private void FightStartHandler(WoWUnit unit, CancelEventArgs cancelable)
    {
        wManager.wManagerSetting.CurrentSetting.CalcuCombatRange = false;
    }

    private void FightLoopHandler(WoWUnit woWPlayer, CancelEventArgs cancelable)
    {
        // Switch target if attacked by other faction player
        WoWPlayer player = ObjectManager.GetNearestWoWPlayer(ObjectManager.GetObjectWoWPlayer().Where(o => o.IsAttackable).ToList());
        if (player == null || !player.IsValid || !player.IsAlive || player.Faction == ObjectManager.Me.Faction || player.IsFlying || player.IsMyTarget || woWPlayer.Guid == player.Guid)
            return;
        if (player.InCombatWithMe && ObjectManager.Target.Type != WoWObjectType.Player)
        {
            cancelable.Cancel = true;
            Fight.StartFight(player.Guid, robotManager.Products.Products.ProductName != "WRotation", false);
        }
    }

    private void EventsWithArgsHandler(string id, List<string> args)
    {
      if (selectedRotation is Hunter)
      {
        if (id == "UNIT_SPELLCAST_SUCCEEDED")
        {
          if (args[0] == "player" && args[1] == "Auto Shot")
            Hunter.LastAuto = DateTime.Now;
        }

        if (id == "UI_ERROR_MESSAGE")
        {
          if (args.Count > 1)
          {
            string message = args[1];
            if (message == "Your pet is not dead.")
              isPetDead = false;
            if (message == "Your pet is dead.")
              isPetDead = true;
          }
        }
      }

      if (selectedRotation is Paladin)
        {
          if (id == "COMBAT_LOG_EVENT_UNFILTERED")
          {
            var args_ = Lua.LuaDoString<List<string>>(@"if (argsForCombatLogEventPuller) then return unpack(argsForCombatLogEventPuller) end");
            if (args_.Count > 1)
              if (args_[1] == "SPELL_CAST_SUCCESS" && (args_[12] == "Blessing of Might" || args_[12] == "Blessing of Kings" || args_[12] == "Blessing of Wisdom"))
                Paladin.RecordBlessingCast(args_[4], args_[12], args_[8]);
          }
        }
    }
}