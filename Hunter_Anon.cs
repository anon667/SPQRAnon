using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Anthrax;
using Anthrax.WoW.Classes.ObjectManager;
using Anthrax.WoW.Internals;
using Anthrax.WoW.Classes;
using System.Runtime.InteropServices;

namespace Anthrax
{
        
    public class Hunter_Anon : Modules.ICombat
    {
        public static int currentTick = 0;
        public static int lastActionTick = 0;

        #region Rule stuff

        public enum RuleUnitType
        {
            Self = 0
            , Target
            , Pet
        }

        public enum RuleAttribute
        {
            Health = 0
            , Focus
            , Mana
            , Buff
            , BuffTimerLeft
            , Debuff
            , DebuffTimerLeft
            , KeyDown
        }

        public enum DebuffIDs : int
        {
            // Hunter
            SerpentSting = 118253,//1978,
            WidowVenom = 82654,
        }

        public enum BuffIDs : int
        {
            // Hunter
            SteadyFocus = 53220,//53224
            InstantAimedShot = 82926,
            ThrillOfTheHunt = 34720,
            ArcaneIntensity = 142978,
        }

        public class Rule
        {
            public int id;
            public RuleUnitType unitType;
            public RuleAttribute attribute;
            public int attributeValue;
            public bool isPercentValue = false;

            public int buffID = 0;

            public bool Validate()
            {
                WowUnit unit = null;

                if (unitType == RuleUnitType.Self)
                {
                    unit = Anthrax.WoW.Internals.ObjectManager.LocalPlayer;
                }
                else if (unitType == RuleUnitType.Target)
                {
                    unit = Anthrax.WoW.Internals.ObjectManager.Target;
                }

                switch (attribute)
                {
                    case RuleAttribute.Focus:
                        if (attributeValue > 0)
                            return unit.GetPower(WowUnit.WowPowerType.Focus) > attributeValue;
                        else
                            return unit.GetPower(WowUnit.WowPowerType.Focus) < Math.Abs(attributeValue);

                    case RuleAttribute.Buff:
                    case RuleAttribute.Debuff:
                        //Log("checking buff/debuff");
                        return unit.HasAuraById(buffID);

                    case RuleAttribute.BuffTimerLeft:
                    case RuleAttribute.DebuffTimerLeft:
                        //Log("checking buff/debuff timer");
                        var buff = unit.Auras.Where(x => x.SpellId == buffID).FirstOrDefault();

                        if (buff == null)
                            return true;

                        return (buff.TimeLeft < attributeValue);

                    case RuleAttribute.Health:
                        if (isPercentValue)
                        {
                            if (attributeValue <= 0)
                                return unit.HealthPercent <= Math.Abs(attributeValue);
                            else
                                return unit.HealthPercent >= attributeValue;
                        }
                        else
                        {
                            if (attributeValue <= 0)
                                return unit.Health <= Math.Abs(attributeValue);
                            else
                                return unit.Health >= attributeValue;
                        }

                    case RuleAttribute.KeyDown:
                        //Log("Checking key down - " + attributeValue + ": " + GetKeyState(attributeValue));
                        return (GetKeyState(attributeValue) < 0);

                }

                return false;
            }

            public static bool operator & (Rule r1, Rule r2)
            {
                return r1.Validate() && r2.Validate();
            }

            public static bool operator | (Rule r1, Rule r2)
            {
                return r1.Validate() || r2.Validate();
            }

            public static bool operator & (Rule r1, bool r2)
            {
                return r1.Validate() && r2;
            }

            public static bool operator | (Rule r1, bool r2)
            {
                return r1.Validate() || r2;
            }

            public static bool operator & (bool r1, Rule r2)
            {
                return r1 && r2.Validate();
            }

            public static bool operator | (bool r1, Rule r2)
            {
                return r1 || r2.Validate();
            }

            public static bool operator ! (Rule r)
            {
                return !r.Validate();
            }
        }

        #endregion


        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern short GetKeyState(int virtualKeyCode);

        public enum Keys : int
        {
            Shift = 0x10,
            Ctrl = 0x11,
            Alt = 0x12,

            Z = 0x5A,
            X = 0x58,
            C = 0x43,
            S = 0x53,
        }

        public enum Spells : int                      //This is a convenient list of all spells used by our combat routine
        {                                               //you can have search on wowhead.com for spell name, and get the id in url
            AimedShot = 19434,
            AimedShot_Instant = 82928,
            ArcaneShot = 3044,
            AutoShot = 75,
            BindingShot = 109248,
            Camouflage = 51753,
            ChimeraShot = 53209,
            ConcussiveShot = 5116,
            Deterrence = 19263,
            Disengage = 781,
            GlaiveToss = 117050,
            HuntersMark = 1130,
            KillShot = 53351,
            MastersCall = 53271,
            MendPet = 136,
            MultiShot = 2643,
            ScareBeast = 1513,
            ScatterShot = 19503,
            SerpentSting = 1978,
            SilencingShot = 34490,
            Stampede = 121818,
            SteadyShot = 56641,
            TranquilizingShot = 19801,
            WidowVenom = 82654,

            CobraShot = 77767,
            ExplosiveShot = 53301,
            
            BlackArrow = 3674,

            KillCommand = 34026,
            Fervor = 82726,
            DireBeast = 120679,
            AMurderOfCrows = 131894,            
        }

        public static class RuleBank
        {
            // Key Rules
            public static Rule KEY_Z_PRESSED = new Rule() { id = 17, attribute = RuleAttribute.KeyDown, attributeValue = (int)Keys.Z};
            public static Rule KEY_S_PRESSED = new Rule() { id = 17, attribute = RuleAttribute.KeyDown, attributeValue = (int)Keys.S };

            // Health Rules
            public static Rule ENEMY_HEALTH_BELOW_OR_EQUAL_20 = new Rule() { id = 1, unitType = RuleUnitType.Target, attribute = RuleAttribute.Health, attributeValue = -20, isPercentValue = true };

            // Focus rules
            public static Rule FOCUS_UNDER_35 = new Rule() { id = 2, unitType = RuleUnitType.Self, attribute = RuleAttribute.Focus, attributeValue = -35 };
            public static Rule FOCUS_OVER_45 = new Rule() { id = 2, unitType = RuleUnitType.Self, attribute = RuleAttribute.Focus, attributeValue = 45 };
            public static Rule FOCUS_OVER_55 = new Rule() { id = 3, unitType = RuleUnitType.Self, attribute = RuleAttribute.Focus, attributeValue = 60 };
            public static Rule FOCUS_OVER_60 = new Rule() { id = 4, unitType = RuleUnitType.Self, attribute = RuleAttribute.Focus, attributeValue = 60 };            
            public static Rule FOCUS_OVER_75 = new Rule() { id = 5, unitType = RuleUnitType.Self, attribute = RuleAttribute.Focus, attributeValue = 75 };

            // Hunter rules
            public static Rule BUFF_ARCANE_INTENSITY = new Rule() { id = 18, unitType = RuleUnitType.Self, attribute = RuleAttribute.Buff, buffID = (int)BuffIDs.ArcaneIntensity };
            public static Rule BUFF_ARCANE_INTENSITY_NEEDS_RECAST = new Rule() { id = 18, unitType = RuleUnitType.Self, attribute = RuleAttribute.BuffTimerLeft, buffID = (int)BuffIDs.ArcaneIntensity, attributeValue = 1500 };

            public static Rule BUFF_INSTANT_AIMED_SHOT = new Rule() { id = 6, unitType = RuleUnitType.Self, attribute = RuleAttribute.Buff, buffID = (int)BuffIDs.InstantAimedShot };
            
            public static Rule BUFF_THRILL_OF_THE_HUNT = new Rule() { id = 7, unitType = RuleUnitType.Self, attribute = RuleAttribute.Buff, buffID = (int)BuffIDs.ThrillOfTheHunt };            
            
            public static Rule BUFF_STEADY_FOCUS = new Rule() { id = 9, unitType = RuleUnitType.Self, attribute = RuleAttribute.Buff, buffID = (int)BuffIDs.SteadyFocus };
            public static Rule BUFF_STEADY_FOCUS_NEEDS_RECAST = new Rule() { id = 10, unitType = RuleUnitType.Self, attribute = RuleAttribute.BuffTimerLeft, buffID = (int)BuffIDs.SteadyFocus, attributeValue = 4000 };
            
            public static Rule DEBUFF_SERPENT_STING = new Rule() { id = 11, unitType = RuleUnitType.Target, attribute = RuleAttribute.Debuff, buffID = (int)DebuffIDs.SerpentSting };
            public static Rule DEBUFF_SERPENT_STING_NEEDS_RECAST = new Rule() { id = 13, unitType = RuleUnitType.Target, attribute = RuleAttribute.DebuffTimerLeft, buffID = (int)DebuffIDs.SerpentSting, attributeValue = 4000 };
            
            public static Rule DEBUFF_WIDOW_VENOM = new Rule() { id = 14, unitType = RuleUnitType.Target, attribute = RuleAttribute.Debuff, buffID = (int)DebuffIDs.WidowVenom };
            public static Rule DEBUFF_WIDOW_VENOM_NEEDS_RECAST = new Rule() { id = 16, unitType = RuleUnitType.Target, attribute = RuleAttribute.DebuffTimerLeft, buffID = (int)DebuffIDs.WidowVenom, attributeValue = 4000 };
        }

        public static Rotation rotation = new Rotation();

        public class Rotation
        {
            public class RotationStep
            {
                public int id;
                public int spellId;
                public bool onGCD;
                public bool canCancelCast;
                public bool debug;
                public bool WaitForGCD;
                public int intervalBetweenExecution;
                public DateTime lastExecution;
                
                public delegate bool RulesExpression();
                public RulesExpression RuleValidatorDel = () => { return true; };

                public RotationStep()
                {
                    id = 0;
                    spellId = 0;
                    onGCD = false;
                    canCancelCast = false;
                    debug = false;
                    WaitForGCD = true;
                    intervalBetweenExecution = 0;
                    lastExecution = DateTime.Now;
                }

                public bool CheckCancel()
                {
                    if (canCancelCast)
                    {
                        var player = Anthrax.WoW.Internals.ObjectManager.LocalPlayer;

                        if (player.IsCasting)
                        {
                            Anthrax.AI.Controllers.Spell.StopCasting();
                            return true;
                        }
                        else
                        {

                        }
                    }

                    return false;
                }

                public bool HasValidRules()
                {
                    return RuleValidatorDel();
                }
            }

            public List<RotationStep> steps = new List<RotationStep>();

            public void Setup_MM_Rotation()
            {
                Stopwatch sw = new Stopwatch();

                sw.Start();

                steps.Clear();

                RotationStep rs = null;

                // Concussive shot
                rs = new RotationStep() { spellId = (int)Spells.ConcussiveShot };
                rs.id = 0;
                rs.RuleValidatorDel = () => { return RuleBank.KEY_Z_PRESSED.Validate(); };
                steps.Add(rs);

                // Silencing shot
                rs = new RotationStep() { spellId = (int)Spells.SilencingShot };
                rs.id = 0;
                rs.RuleValidatorDel = () => { return RuleBank.KEY_S_PRESSED.Validate(); };
                rs.canCancelCast = true;
                steps.Add(rs);

                // Instant aimed shot
                rs = new RotationStep() { spellId = (int)Spells.AimedShot_Instant };
                rs.id = 1;
                rs.RuleValidatorDel = () => { return RuleBank.BUFF_INSTANT_AIMED_SHOT.Validate(); };
                steps.Add(rs);

                // Chimera Shot
                rs = new RotationStep() { spellId = (int)Spells.ChimeraShot };
                rs.id = 5;
                rs.RuleValidatorDel = () => { return RuleBank.FOCUS_OVER_60.Validate(); };
                steps.Add(rs);

                // Kill Shot
                rs = new RotationStep() { spellId = (int)Spells.KillShot };
                rs.id = 2;
                rs.RuleValidatorDel = () => { return RuleBank.ENEMY_HEALTH_BELOW_OR_EQUAL_20.Validate(); };
                steps.Add(rs);

                //// Glaive toss
                rs = new RotationStep() { spellId = (int)Spells.GlaiveToss };
                rs.id = 7;
                rs.RuleValidatorDel = () => { return RuleBank.FOCUS_OVER_45.Validate(); };
                steps.Add(rs);

                // Steady shot (for attack speed buff)
                rs = new RotationStep() { spellId = (int)Spells.SteadyShot };
                rs.id = 3;
                rs.RuleValidatorDel = () => { return !RuleBank.BUFF_STEADY_FOCUS | (RuleBank.BUFF_STEADY_FOCUS & RuleBank.BUFF_STEADY_FOCUS_NEEDS_RECAST); };
                steps.Add(rs);

                // Arcane shot (with -focus buff)
                rs = new RotationStep() { spellId = (int)Spells.ArcaneShot };
                rs.id = 8;
                rs.RuleValidatorDel = () => { return RuleBank.BUFF_THRILL_OF_THE_HUNT & (!RuleBank.BUFF_ARCANE_INTENSITY | RuleBank.BUFF_ARCANE_INTENSITY_NEEDS_RECAST); };
                rs.WaitForGCD = true;
                steps.Add(rs);

                // Serpent Sting
                rs = new RotationStep() { spellId = (int)Spells.SerpentSting, intervalBetweenExecution = 1500 };
                rs.id = 4;
                rs.RuleValidatorDel = () => { return RuleBank.FOCUS_OVER_45 & (!RuleBank.DEBUFF_SERPENT_STING | (RuleBank.DEBUFF_SERPENT_STING & RuleBank.DEBUFF_SERPENT_STING_NEEDS_RECAST)); };
                steps.Add(rs);

                //// Widow Venom
                rs = new RotationStep() { spellId = (int)Spells.WidowVenom };
                rs.id = 6;
                rs.RuleValidatorDel = () => { return RuleBank.FOCUS_OVER_60 & (!RuleBank.DEBUFF_WIDOW_VENOM | (RuleBank.DEBUFF_WIDOW_VENOM & RuleBank.DEBUFF_WIDOW_VENOM_NEEDS_RECAST)); };
                steps.Add(rs);

                //// Default Steady shot
                rs = new RotationStep() { spellId = (int)Spells.SteadyShot, intervalBetweenExecution = 50 };
                rs.id = 9;
                rs.WaitForGCD = true;
                steps.Add(rs);

                //Anthrax.WoW.Internals.ObjectManager.LocalPlayer.

                //steps.Add(new RotationStep((int)Spells.SteadyShot, true, true));
                sw.Stop();

                Log("Total time:" + sw.ElapsedMilliseconds.ToString());
            }
        }        

        public override string Name
        {
            get { return "Hunter - Anon"; }
        }
      
        public override void OnPull(WoW.Classes.ObjectManager.WowUnit unit)
        {

        }

        //public int IsCasting()
        //{
        //    return Anthrax.WoW.Internals.ObjectManager.LocalPlayer.CastingSpellId;
        //}

        // /!\ WARNING /!\
        // The OnCombat function should NOT be blocking function !
        // The bot will handle calling it in loop until the combat is over.
        // Blocking function may lead to slow behavior.
        public override void OnCombat(WoW.Classes.ObjectManager.WowUnit unit)
        {
            ++currentTick;

            if (currentTick - lastActionTick < 5)
                return;

            var target = Anthrax.WoW.Internals.ObjectManager.Target;
            var player = Anthrax.WoW.Internals.ObjectManager.LocalPlayer;
            //var castId = IsCasting();

            //if ((GetAsyncKeyState(0x10) == -32767))
            //{
            //    Anthrax.AI.Controllers.Spell.Cast(13813, target); // Explosive trap spell id : http://www.wowhead.com/spell=13813
            //    Anthrax.WoW.Internals.Bindings.ActionPress("CAMERAORSELECTORMOVE");
            //    return;
            //    //   changeRotation();
            //}

            //if ((GetKeyState((int)Keys.Shift) < 0))
            //{
            //    WoW.Internals.ActionBar.ExecuteSpell(((int)Spells.MultiShot));
            //    return;
            //}            

            if (!target.IsValid || target.Health < 1 || !player.InCombat)
                return;

            foreach (Rotation.RotationStep step in rotation.steps)
            {
                if (step.HasValidRules())
                {
                    if (step.intervalBetweenExecution > 0 && (DateTime.Now - step.lastExecution).TotalMilliseconds < step.intervalBetweenExecution)
                    {
                        continue;
                    }

                    // Can't cast because of GCD
                    if (Cooldown.IsGlobalCooldownActive == false && step.WaitForGCD == false)
                    {
                        //continue;
                        return;
                    }

                    var cd = WoW.Internals.Cooldown.GetCooldowns().Where(x => x.SpellId == step.spellId).FirstOrDefault();

                    if (cd != null)
                        if (cd.TimeLeft > 1500)
                            continue;

                    int counter = 0;
                    while (!AI.Controllers.Spell.CanCast(step.spellId))
                    {
                        System.Threading.Thread.Sleep(25);
                        ++counter;

                        if (counter > 60)
                        {
                            return;
                        }                            
                    }
                        
                    //WowCooldown cd = WoW.Internals.Cooldown.GetCooldowns().Where(x => x.SpellId == step.spellId).FirstOrDefault();
                    //Log("can cast - " + AI.Controllers.Spell.CanCast(step.spellId));
                    //if (cd != null)
                    //{
                    //    Log("has cd - " + cd.TimeLeft);
                    //    Log("gcd    - " + WoW.Internals.Cooldown.IsGlobalCooldownActive);
                    //    if (cd.TimeLeft > 0 && cd.TimeLeft > 1500)
                    //    {
                    //        Log("cd greater than 1.5k ms");
                    //        continue;
                    //    }
                    //}
                    Log("Casting " + step.id.ToString());

                    //int counter = 0;
                    //while (counter < 3)
                    //{
                        WoW.Internals.ActionBar.ExecuteSpell(step.spellId);
                    //    System.Threading.Thread.Sleep(25);
                    //    ++counter;
                    //}

                        lastActionTick = currentTick;
                    step.lastExecution = DateTime.Now;
                    
                    return;
                }
                else
                {
                 //  Log("Not valid. Skipping.");
                }
            }
        }

        public override void OnLoad()
        {
            rotation.Setup_MM_Rotation();
            Log("Loaded anon MM Hunter");
            //rotation.Print();
        }

        public static void Log(string t)
        {
            Anthrax.Logger.WriteLine(t);
        }

    }
}
