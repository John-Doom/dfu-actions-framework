using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Questing;
using DaggerfallWorkshop.Game.Entity;
using System.Text.RegularExpressions;
using FullSerializer;
using UnityEngine;
using DaggerfallWorkshop;

namespace ActionsMod
{
    public class Actions
    {
        static Mod mod;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            QuestMachine questMachine = GameManager.Instance.QuestMachine;
            questMachine.RegisterAction(new ReducePlayerHealth(null));
            questMachine.RegisterAction(new WithinUnits(null));

            mod.IsReady = true;
        }
    }

    //reduce player health by X
    //This would be a number from 1 to 100, representing a percentage of the player's current health
    public class ReducePlayerHealth : ActionTemplate
    {
        int percent;

        public ReducePlayerHealth(Quest parentQuest) : base(parentQuest)
        {
        }

        public override string Pattern
        {
            get { return @"reduce player health by (?<percent>\d+)"; }
        }

        public override IQuestAction CreateNew(string source, Quest parentQuest)
        {
            Match match = Test(source);
            if (!match.Success) return null;

            ReducePlayerHealth action = new ReducePlayerHealth(parentQuest);
            action.percent = Parser.ParseInt(match.Groups["percent"].Value);
            return action;
        }

        public override void Update(Task caller)
        {
            PlayerEntity player = GameManager.Instance.PlayerEntity;
            int health = player.CurrentHealth - (int)((float)player.MaxHealth / 100f * (float)percent);
            if (health < 1) health = 1;
            player.SetHealth(health);

            SetComplete();
        }

        [fsObject("v1")]
        public struct SaveData_v1
        {
            public int percent;
        }

        public override object GetSaveData()
        {
            SaveData_v1 data = new SaveData_v1();
            data.percent = percent;

            return data;
        }

        public override void RestoreSaveData(object dataIn)
        {
            if (dataIn == null) return;

            SaveData_v1 data = (SaveData_v1)dataIn;
            percent = data.percent;
        }
    }

    //player within 5 units of foe/item _symbol_
    public class WithinUnits : ActionTemplate
    {
        int distance;
        Symbol symbol;

        public WithinUnits(Quest parentQuest) : base(parentQuest)
        {
            IsTriggerCondition = true;
        }

        public override string Pattern
        {
            get
            {
                return
                    @"player within (?<distance>\d+) units of foe (?<foe>[a-zA-Z0-9_.-]+)|"+
                    @"player within (?<distance>\d+) units of item (?<item>[a-zA-Z0-9_.-]+)";
            }
        }

        public override IQuestAction CreateNew(string source, Quest parentQuest)
        {
            Match match = Test(source);
            if (!match.Success) return null;

            WithinUnits action = new WithinUnits(parentQuest);

            action.distance = Parser.ParseInt(match.Groups["distance"].Value);

            Group group = match.Groups["foe"];
            if (group.Success) action.symbol = new Symbol(group.Value);
            else action.symbol = new Symbol(match.Groups["item"].Value);

            return action;
        }

        public override bool CheckTrigger(Task caller)
        {
            QuestResource res = ParentQuest.GetResource(symbol);
            if (res == null || res.QuestResourceBehaviour == null) return false;

            Vector3 resPos = res.QuestResourceBehaviour.transform.position;
            Vector2 resPos2D = new Vector2(resPos.x, resPos.z);

            Vector3 playerPos = GameManager.Instance.PlayerGPS.transform.position;
            Vector2 playerPos2D = new Vector2(playerPos.x, playerPos.z);

            //TODO: add raycast check

            return
                Vector2.Distance(resPos2D, playerPos2D) <= distance &&
                Mathf.Abs(resPos.y - playerPos.y) <= 1f;
        }

        [fsObject("v1")]
        public struct SaveData_v1
        {
            public int distance;
            public Symbol symbol;
        }

        public override object GetSaveData()
        {
            SaveData_v1 data = new SaveData_v1();
            data.distance = distance;
            data.symbol = symbol;

            return data;
        }

        public override void RestoreSaveData(object dataIn)
        {
            if (dataIn == null) return;

            SaveData_v1 data = (SaveData_v1)dataIn;
            distance = data.distance;
            symbol = data.symbol;
        }
    }
}
