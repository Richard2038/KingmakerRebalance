﻿using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Designers.Mechanics.Buffs;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallOfTheWild
{
    public partial class HexEngine
    {
        //general hexes:
        //chant = cackle
        //draconic resilence - done
        //evil eye, fortune, misfirtune, ward, healing - same; + witch hex
        //fury - done
        //intimidating display - give feat - done
        //secret - give metamagic feat - done
        //shapeshift  - give spells in 1 minute increments - done
        //wings - attack at lvl1, normal wings at lvl 8 - done


        BlueprintFeature CreateWingsAttackHex(string name_prefix, string display_name, string description)
        {
            var wing_weapon = library.Get<BlueprintItemWeapon>("864e29d3e07ad4a4f96d576b366b4a86");//wing 1d4

            var ability = library.CopyAndAdd<BlueprintActivatableAbility>("7679910a16368cc43b496cef2babe1cb", name_prefix + "HexActivatableAbility", ""); //silver dragon wings

            var buff = Helpers.CreateBuff(name_prefix + "HexBuff",
                                          display_name,
                                          description,
                                          "",
                                          ability.Icon,
                                          ability.Buff.FxOnStart,
                                          Common.createAddSecondaryAttacks(wing_weapon)
                                          );

            ability.Buff = buff;

            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            feature.Ranks = 1;
            return feature;
        }


        BlueprintFeature CreateWingsHex(string name_prefix, string display_name, string description)
        {
            var ability = library.Get<BlueprintActivatableAbility>("7679910a16368cc43b496cef2babe1cb"); //silver dragon wings
            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  display_name,
                                                  description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            feature.Ranks = 1;
            return feature;
        }


        BlueprintFeatureSelection createSecret(string name_prefix, string display_name, string description)
        {
            var metamagic_feats = library.GetAllBlueprints().OfType<BlueprintFeature>().Where(b => b.Groups.Contains(FeatureGroup.WizardFeat) && (b.GetComponent<AddMetamagicFeat>() != null));

            var feature = Helpers.CreateFeatureSelection(name_prefix + "HexFeature",
                                      display_name,
                                      description,
                                      "",
                                      null,
                                      FeatureGroup.None);
            feature.Ranks = 1;
            feature.AddComponent(Helpers.PrerequisiteNoFeature(feature));
            feature.AllFeatures = metamagic_feats.ToArray();

            return feature;
        }


        BlueprintFeatureSelection createIntimidatingDisplay(string name_prefix, string display_name, string description)
        {
            var dazzling_display_feature = library.Get<BlueprintAbility>("bcbd674ec70ff6f4894bb5f07b6f4095");
            var feature = Helpers.CreateFeatureSelection(name_prefix + "HexFeature",
                          display_name,
                          description,
                          "",
                          null,
                          FeatureGroup.None,
                          Helpers.CreateAddFact(dazzling_display_feature));
            feature.Ranks = 1;

            
            var dazzling_display = library.Get<BlueprintAbility>("5f3126d4120b2b244a95cb2ec23d69fb");
            dazzling_display.GetComponent<NewMechanics.AbilityCasterMainWeaponCheckHasParametrizedFeature>().alternative = feature; 

            return feature;
        }


        BlueprintFeature createShapeshiftHex(string name_prefix, string display_name, string description)
        {
            var resource = Helpers.CreateAbilityResource(name_prefix + "HexResource", "", "", "", null);
            resource.SetIncreasedByLevel(0, 1, hex_classes);
            BlueprintAbility[] shapes = new BlueprintAbility[] {Wildshape.wolf_form_spell, Wildshape.leopard_form_spell, Wildshape.bear_form_spell, Wildshape.dire_wolf_form_spell,
                                                               Wildshape.smilodon_form_spell, Wildshape.mastodon_form_spell, Wildshape.hodag_form_spell, Wildshape.winter_wolf_form_spell};
            int[] levels = new int[] { 8, 8, 12, 12, 16, 16, 20, 20 };

            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                      display_name,
                                      description,
                                      "",
                                      Wildshape.wolf_form_spell.Icon,
                                      FeatureGroup.None,
                                      Helpers.CreateAddAbilityResource(resource));
            feature.Ranks = 1;
            var minute_duration = Helpers.CreateContextDuration(1, DurationRate.Minutes);
            for (int i = 0; i < shapes.Length; i++)
            {
                var ability_i = library.CopyAndAdd<BlueprintAbility>(shapes[i], name_prefix + shapes[i].name, "");
                ability_i.AddComponent(Helpers.CreateResourceLogic(resource));
                ability_i.Type = AbilityType.Supernatural;
                ability_i.LocalizedDuration = Helpers.CreateString(ability_i.name + ".Duration", Helpers.oneMinuteDuration);
                ability_i.ReplaceComponent<ContextRankConfig>(Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, classes: hex_classes));
                ability_i.AddComponent(Common.createContextCalculateAbilityParamsBasedOnClasses(hex_classes, hex_stat));
                ability_i.ReplaceComponent<AbilityEffectRunAction>(a => a.Actions = Helpers.CreateActionList(Common.changeAction<ContextActionApplyBuff>(a.Actions.Actions, b => b.DurationValue = minute_duration)));
                var feature_i = Common.AbilityToFeature(ability_i);

                feature.AddComponent(Helpers.CreateAddFeatureOnClassLevel(feature_i, levels[i], hex_classes));
            }

            return feature;
        }


        BlueprintFeature createDraconicResilence(string name_prefix, string display_name, string description)
        {
            var icon = library.Get<BlueprintAbility>("f767399367df54645ac620ef7b2062bb").Icon; //form of the dragon

            var buff1 = Helpers.CreateBuff(name_prefix + "1HexBuff",
                                           display_name,
                                           description,
                                           "",
                                           icon,
                                           null,
                                           Common.createBuffDescriptorImmunity(SpellDescriptor.Sleep),
                                           Common.createSpellImmunityToSpellDescriptor(SpellDescriptor.Sleep)
                                           );

            var buff2 = library.CopyAndAdd<BlueprintBuff>(buff1, name_prefix + "2HexBuff", "");
            buff2.AddComponents(Common.createBuffDescriptorImmunity(SpellDescriptor.Paralysis),
                                Common.createSpellImmunityToSpellDescriptor(SpellDescriptor.Paralysis)
                                );


            var apply1 = Common.createContextActionApplyBuff(buff1, Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.Default)), dispellable: false);
            var apply7 = Common.createContextActionApplyBuff(buff2, Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.Default)), dispellable: false);
          

            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Touch,
                                                Helpers.roundsPerLevelDuration,
                                                "",
                                                Helpers.CreateRunActions(Common.createRunActionsDependingOnContextValue(Helpers.CreateContextValue(AbilityRankType.StatBonus),
                                                                                                                        Helpers.CreateActionList(apply1),
                                                                                                                        Helpers.CreateActionList(apply7))
                                                                        ),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.OnePlusDivStep,
                                                                           type: AbilityRankType.StatBonus, stepLevel: 7, min: 1, max: 2, classes: hex_classes),
                                                Common.createAbilitySpawnFx("c4d861e816edd6f4eab73c55a18fdadd", anchor: AbilitySpawnFxAnchor.SelectedTarget)
                                               );
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);
            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            feature.Ranks = 1;
            return feature;
        }


        BlueprintFeature createFury(string name_prefix, string display_name, string description)
        {
            var icon = library.Get<BlueprintAbility>("97b991256e43bb140b263c326f690ce2").Icon; //rage

            var buff = Helpers.CreateBuff(name_prefix + "HexBuff",
                                           display_name,
                                           description,
                                           "",
                                           icon,
                                           null,
                                           Helpers.CreateAddContextStatBonus(StatType.AdditionalDamage, ModifierDescriptor.Morale, rankType: AbilityRankType.StatBonus),
                                           Helpers.CreateAddContextStatBonus(StatType.SaveFortitude, ModifierDescriptor.Resistance, rankType: AbilityRankType.StatBonus),
                                           Helpers.CreateAddContextStatBonus(StatType.SaveReflex, ModifierDescriptor.Resistance, rankType: AbilityRankType.StatBonus),
                                           Helpers.CreateAddContextStatBonus(StatType.SaveWill, ModifierDescriptor.Resistance, rankType: AbilityRankType.StatBonus),
                                           Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.OnePlusDivStep,
                                                                           type: AbilityRankType.StatBonus, startLevel: -8, stepLevel: 8, classes: hex_classes)
                                           );

            var apply_buff = Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.SpeedBonus)), dispellable: false);

            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Touch,
                                                "Variable",
                                                "",
                                                Helpers.CreateRunActions(apply_buff),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.StatBonus, progression: ContextRankProgression.AsIs,
                                                                           type: AbilityRankType.SpeedBonus, stat: hex_stat),
                                                Common.createAbilitySpawnFx("97b991256e43bb140b263c326f690ce2", anchor: AbilitySpawnFxAnchor.SelectedTarget)
                                               );

            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);
            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            feature.Ranks = 1;
            return feature;
        }



        //battle spirit hexes
        public BlueprintFeature createBattleWardHex(string name_prefix, string display_name, string description)
        {
            var shield_spell = library.Get<BlueprintAbility>("ef768022b0785eb43a18969903c537c4"); //shield spell

            var ac_buffs = new BlueprintBuff[5];
            var actions = new ActionList[5];

            for (int i = 0; i < ac_buffs.Length; i++)
            {
                var on_attack_action = i == 0 ? Helpers.CreateActionList(Helpers.Create<ContextActionRemoveSelf>()) 
                                              : Helpers.CreateActionList(Helpers.Create<ContextActionRemoveSelf>(), actions[i - 1].Actions[0]);
                ac_buffs[i] = Helpers.CreateBuff(name_prefix + $"{i+1}Buff",
                                                display_name,
                                                description + $" (+{i+1})",
                                                "",
                                                shield_spell.Icon,
                                                null,
                                                Helpers.CreateAddStatBonus(StatType.AC, (i + 1), ModifierDescriptor.Deflection),
                                                Helpers.Create<AddTargetAttackRollTrigger>(a => {
                                                    a.ActionsOnAttacker = Helpers.CreateActionList(); a.OnlyHit = false;
                                                    a.ActionOnSelf = on_attack_action;
                                                })                                                    
                                                );
                actions[i] = Helpers.CreateActionList(Common.createContextActionApplyBuff(ac_buffs[i], Helpers.CreateContextDuration(), dispellable: false, is_permanent: true));
            }

            var hex_ability = library.CopyAndAdd<BlueprintAbility>(shield_spell, name_prefix + "HexAbility", "");
            hex_ability.RemoveComponents<SpellListComponent>();
            hex_ability.RemoveComponents<SpellComponent>();
            hex_ability.Type = AbilityType.Supernatural;
            hex_ability.setMiscAbilityParametersTouchFriendly();
            var effect = Common.createRunActionsDependingOnContextValue(Helpers.CreateContextValue(AbilityRankType.StatBonus), actions);
            hex_ability.ReplaceComponent<AbilityEffectRunAction>(Helpers.CreateRunActions(effect));
            hex_ability.AddComponent(Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.StartPlusDivStep,
                                                                     type: AbilityRankType.StatBonus, startLevel: -16, stepLevel: 8, classes: hex_classes));
            hex_ability.SetIcon(shield_spell.Icon);
            hex_ability.SetName(display_name);
            hex_ability.SetDescription(description);
            addWitchHexCooldownScaling(hex_ability, "");

            var battle_ward = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  hex_ability.Name,
                                                  hex_ability.Description,
                                                  "",
                                                  hex_ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(hex_ability));
            battle_ward.Ranks = 1;
            return battle_ward;
        }


        BlueprintFeature createHamperingHex(string name_prefix, string display_name, string description)
        {
            var haze_of_dreams = library.Get<BlueprintAbility>("40ec382849b60504d88946df46a10f2d");

            var buff = Helpers.CreateBuff(name_prefix + "Buff",
                                          display_name,
                                          description,
                                          "",
                                          haze_of_dreams.Icon,
                                          null,
                                          Helpers.CreateAddContextStatBonus(StatType.AC, ModifierDescriptor.None, ContextValueType.Rank, AbilityRankType.StatBonus, -2),
                                          Helpers.CreateAddContextStatBonus(StatType.AdditionalCMD, ModifierDescriptor.None, ContextValueType.Rank, AbilityRankType.StatBonus, -2),
                                          Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.StartPlusDivStep,
                                                                           type: AbilityRankType.StatBonus, startLevel: 0, stepLevel: 8, min: 1, max: 2, classes: hex_classes)
                                                                          
                                          );

            var apply_saved = Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(1), dispellable: false);
            var apply_failed = Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.Default)), dispellable: false);
            var action_save = Common.createContextActionSavingThrow(SavingThrowType.Will, Helpers.CreateActionList(Helpers.CreateConditionalSaved(apply_saved, apply_failed)));

            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                buff.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Close,
                                                "Variable",
                                                "Will special",
                                                haze_of_dreams.GetComponent<AbilitySpawnFx>(),
                                                Helpers.CreateRunActions(action_save));
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);

            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var hampering_hex = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            hampering_hex.Ranks = 1;
            return hampering_hex;
        }

              //also a life hex
        BlueprintFeature createCurseOfSuffering(string name_prefix, string display_name, string description)
        {
            var forced_repentance = library.Get<BlueprintAbility>("cc0aeb74b35cb7147bff6c53538bbc76");

            BlueprintBuff[] bleed_buffs = new BlueprintBuff[]
            {
                library.Get<BlueprintBuff>("5eb68bfe186d71a438d4f85579ce40c1"),
                library.Get<BlueprintBuff>("75039846c3d85d940aa96c249b97e562"),
                library.Get<BlueprintBuff>("16249b8075ab8684ca105a78a047a5ef"),
                library.Get<BlueprintBuff>("f80de2a32fc2a7141b23ec29bc36f395") //constitution
            };

            var buff = Helpers.CreateBuff(name_prefix + "Buff",
                                          display_name,
                                          description,
                                          "",
                                          forced_repentance.Icon,
                                          null,
                                          Helpers.Create<HealingMechanics.IncomingHealingModifier>(i => i.ModifierPercents = 50)
                                          );

            foreach (var bb in bleed_buffs)
            {
                var context_actions = bb.GetComponent<AddFactContextActions>();
                ContextActionDealDamage dmg_action = context_actions.NewRound.Actions.Where(a => a is ContextActionDealDamage).FirstOrDefault() as ContextActionDealDamage;
                dmg_action = dmg_action.CreateCopy();
                dmg_action.Value = Helpers.CreateContextDiceValue(Kingmaker.RuleSystem.DiceType.Zero, bonus: 1);

                context_actions.NewRound = Helpers.CreateActionList(new GameAction[] { dmg_action }.AddToArray(context_actions.NewRound.Actions));
            }
            var apply_buff = Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.Default)), dispellable: false);
            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                buff.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Close,
                                                Helpers.roundsPerLevelDuration,
                                                "Will special",
                                                forced_repentance.GetComponent<AbilitySpawnFx>(),
                                                Helpers.CreateRunActions(apply_buff));
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);

            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var curse_of_suffering = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            curse_of_suffering.Ranks = 1;
            return curse_of_suffering;
        }

        //bones spirit hexes
        public BlueprintFeature createBoneWard(string name_prefix, string display_name, string description)
        {
            var shield_spell = library.Get<BlueprintAbility>("ef768022b0785eb43a18969903c537c4"); //shield spell

            var ac_buffs = new BlueprintBuff[3];

            for (int i = 0; i < ac_buffs.Length; i++)
            {
                ac_buffs[i] = Helpers.CreateBuff(name_prefix + $"{i + 1}Buff",
                                                display_name,
                                                description + $" (+{2 + i})",
                                                "",
                                                shield_spell.Icon,
                                                null,
                                                Helpers.CreateAddStatBonus(StatType.AC, (i + 1), ModifierDescriptor.Deflection)
                                                );
            }

            var action1 = Common.createContextActionApplyBuff(ac_buffs[0], Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.Default)), dispellable: false);
            var action2 = Common.createContextActionApplyBuff(ac_buffs[1], Helpers.CreateContextDuration(1, DurationRate.Minutes), dispellable: false);
            var action3 = Common.createContextActionApplyBuff(ac_buffs[1], Helpers.CreateContextDuration(1, DurationRate.Hours), dispellable: false);

            var hex_ability = library.CopyAndAdd<BlueprintAbility>(shield_spell, name_prefix + "HexAbility", "");
            hex_ability.RemoveComponents<SpellListComponent>();
            hex_ability.RemoveComponents<SpellComponent>();
            hex_ability.Type = AbilityType.Supernatural;
            hex_ability.setMiscAbilityParametersTouchFriendly();


            var effect = Common.createRunActionsDependingOnContextValue(Helpers.CreateContextValue(AbilityRankType.StatBonus),
                                                                        Helpers.CreateActionList(action1),
                                                                        Helpers.CreateActionList(action2),
                                                                        Helpers.CreateActionList(action3)
                                                                        );
            hex_ability.ReplaceComponent<AbilityEffectRunAction>(Helpers.CreateRunActions(effect));
            hex_ability.AddComponent(Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.StartPlusDivStep,
                                                                     type: AbilityRankType.StatBonus, startLevel: 0, stepLevel: 8, classes: hex_classes));
            hex_ability.SetIcon(shield_spell.Icon);
            hex_ability.SetName(display_name);
            hex_ability.SetDescription(description);
            addWitchHexCooldownScaling(hex_ability, "");

            var bone_ward = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  hex_ability.Name,
                                                  hex_ability.Description,
                                                  "",
                                                  hex_ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(hex_ability));
            bone_ward.Ranks = 1;
            return bone_ward;
        }


        public BlueprintFeature createFearfulGaze(string name_prefix, string display_name, string description)
        {
            var fear = library.Get<BlueprintAbility>("d2aeac47450c76347aebbc02e4f463e0");

            var shaken = library.Get<BlueprintBuff>("25ec6cb6ab1845c48a95f9c20b034220");
            var frightened = library.Get<BlueprintBuff>("f08a7239aa961f34c8301518e71d4cdf");

            var apply_shaken = Common.createContextActionApplyBuff(shaken, Helpers.CreateContextDuration(1), dispellable: false);
            var apply_fear = Common.createContextActionApplyBuff(frightened, Helpers.CreateContextDuration(1), dispellable: false);
            var saved_action = Helpers.CreateConditionalSaved(null, Common.createRunActionsDependingOnContextValue(Helpers.CreateContextValue(AbilityRankType.StatBonus),
                                                                                                Helpers.CreateActionList(apply_shaken),
                                                                                                Helpers.CreateActionList(apply_fear)
                                                                                                )
                                                             );

            var action_save = Common.createContextActionSavingThrow(SavingThrowType.Will, Helpers.CreateActionList(saved_action));

            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                fear.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Close,
                                                Helpers.oneRoundDuration,
                                                Helpers.willNegates,
                                                Helpers.CreateRunActions(action_save),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.StartPlusDivStep,
                                                                           type: AbilityRankType.StatBonus, startLevel: 0, stepLevel: 8, min: 1, max: 2, classes: hex_classes),
                                                Helpers.CreateSpellDescriptor(SpellDescriptor.Fear | SpellDescriptor.Shaken | SpellDescriptor.MindAffecting)
                                               );
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);
            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var fearful_gaze = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            fearful_gaze.Ranks = 1;
            return fearful_gaze;
        }


        public BlueprintFeature createBoneLock(string name_prefix, string display_name, string description)
        {
            var boneshaker = library.Get<BlueprintAbility>("b7731c2b4fa1c9844a092329177be4c3");

            var staggered = library.Get<BlueprintBuff>("df3950af5a783bd4d91ab73eb8fa0fd3");
            var staggered_save = library.CopyAndAdd<BlueprintBuff>(staggered, "BoneLockStaggeredSaveEachRound", "");
            staggered_save.ReplaceComponent<BuffStatusCondition>(b => { b.SaveEachRound = true; b.SaveType = SavingThrowType.Fortitude;});

            var apply1 = Common.createContextActionApplyBuff(staggered, Helpers.CreateContextDuration(1), dispellable: false);
            var apply8 = Common.createContextActionApplyBuff(staggered_save, Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.Default)), dispellable: false);
            var apply16 = Common.createContextActionApplyBuff(staggered, Helpers.CreateContextDuration(1), dispellable: false);

            var saved_action = Helpers.CreateConditionalSaved(null, Common.createRunActionsDependingOnContextValue(Helpers.CreateContextValue(AbilityRankType.StatBonus),
                                                                                    Helpers.CreateActionList(apply1),
                                                                                    Helpers.CreateActionList(apply8),
                                                                                    Helpers.CreateActionList(apply16)
                                                                                    )
                                                            );


            var action_save = Common.createContextActionSavingThrow(SavingThrowType.Fortitude, Helpers.CreateActionList(saved_action));

            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                boneshaker.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Close,
                                                Helpers.oneRoundDuration,
                                                Helpers.willNegates,
                                                Helpers.CreateRunActions(action_save),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.StartPlusDivStep,
                                                                           type: AbilityRankType.StatBonus, startLevel: 0, stepLevel: 8, min: 1, max: 3, classes: hex_classes),
                                                Common.createAbilityTargetHasFact(true, library.Get<BlueprintFeature>("fd389783027d63343b4a5634bd81645f")), //construct
                                                Common.createAbilityTargetHasFact(true, library.Get<BlueprintFeature>("3bec99efd9a363242a6c8d9957b75e91")), //aberration
                                                Common.createAbilityTargetHasFact(true, library.Get<BlueprintFeature>("706e61781d692a042b35941f14bc41c5")), //plant
                                                Common.createAbilityTargetHasFact(true, library.Get<BlueprintFeature>("198fd8924dabcb5478d0f78bd453c586")) //elemental
                                               );
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);
            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var bone_lock = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            bone_lock.Ranks = 1;
            return bone_lock;
        }

        //flame spirit hexes
        //will need to add cinder dance
        public BlueprintFeature createFlameWardHex(string name_prefix, string display_name, string description)
        {
            var sacred_nimbus_buff = library.Get<BlueprintBuff>("57b1c6a69c53f4d4ea9baec7d0a3a93a"); //shield spell

            var dmg_buffs = new BlueprintBuff[3];
            var actions = new ActionList[3];

            var dmg = Helpers.CreateActionDealDamage(DamageEnergyType.Fire,
                                                      Helpers.CreateContextDiceValue(Kingmaker.RuleSystem.DiceType.D6, 1, Helpers.CreateContextValue(AbilityRankType.DamageBonus)),
                                                      IgnoreCritical: true);

            var buff = Helpers.CreateBuff(name_prefix + "Buff",
                                          display_name,
                                          description,
                                          "",
                                          sacred_nimbus_buff.Icon,
                                          sacred_nimbus_buff.FxOnStart);

            GameAction[] remove_action = new GameAction[3];

            for (int i = 0; i < dmg_buffs.Length; i++)
            {
                var on_attack_action = i == 0 ? Helpers.CreateActionList(Helpers.Create<ContextActionRemoveSelf>())
                                              : Helpers.CreateActionList(Helpers.Create<ContextActionRemoveSelf>(), actions[i - 1].Actions[0]);
                dmg_buffs[i] = Helpers.CreateBuff(name_prefix + $"{i + 1}Buff",
                                                display_name,
                                                description + $" (+{i + 1})",
                                                "",
                                                sacred_nimbus_buff.Icon,
                                                null,
                                                Common.createAddTargetAttackWithWeaponTrigger(on_attack_action, 
                                                                                              Helpers.CreateActionList(dmg),
                                                                                              not_reach: false),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.Div2,
                                                                           type: AbilityRankType.DamageBonus, classes: hex_classes)
                                                );
                dmg_buffs[i].SetBuffFlags(BuffFlags.HiddenInUi);
                actions[i] = Helpers.CreateActionList(Common.createContextActionApplyBuff(dmg_buffs[i], Helpers.CreateContextDuration(), is_child: true, dispellable: false, is_permanent: true));
                remove_action[i] = Common.createContextActionRemoveBuff(dmg_buffs[i]);
            }


            buff.AddComponents(Helpers.CreateAddFactContextActions(new GameAction[] { Common.createRunActionsDependingOnContextValue(Helpers.CreateContextValue(AbilityRankType.StatBonus), actions) },
                                                                   remove_action),
                                                                   Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.OnePlusDivStep,
                                                                           type: AbilityRankType.StatBonus, stepLevel: 8, classes: hex_classes)
                                                                  );
            var apply_buff = Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(1, DurationRate.Minutes), dispellable: false);
            var hex_ability = Helpers.CreateAbility(name_prefix + "HexAbility",
                                                    display_name,
                                                    description,
                                                    "",
                                                    sacred_nimbus_buff.Icon,
                                                    AbilityType.Supernatural,
                                                    Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                    AbilityRange.Touch,
                                                    "",
                                                    "",
                                                    Helpers.CreateRunActions(apply_buff)
                                                    );

            hex_ability.setMiscAbilityParametersTouchFriendly();
            
            addWitchHexCooldownScaling(hex_ability, "");

            var flame_ward = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  hex_ability.Name,
                                                  hex_ability.Description,
                                                  "",
                                                  hex_ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(hex_ability));
            flame_ward.Ranks = 1;
            return flame_ward;
        }


        public BlueprintFeature createFireNimbus(string name_prefix, string display_name, string description)
        {
            var buff = library.CopyAndAdd<BlueprintBuff>("cc383a9eaae4d2b45a925d442b367b54", name_prefix + "Buff", ""); //faery fire
            buff.AddComponent(Common.createContextSavingThrowBonusAgainstDescriptor(-2, ModifierDescriptor.UntypedStackable, SpellDescriptor.Fire));
            buff.SetNameDescription(display_name, description);
            var apply_buff = Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.Default)), dispellable: false);

            var save = Helpers.CreateConditionalSaved(null, apply_buff);
           
            var action_save = Common.createContextActionSavingThrow(SavingThrowType.Will, Helpers.CreateActionList(apply_buff));

            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                buff.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Close,
                                                Helpers.roundsPerLevelDuration,
                                                Helpers.willNegates,
                                                Helpers.CreateRunActions(action_save)
                                               );
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);
            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var fire_nimbus = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            fire_nimbus.Ranks = 1;
            return fire_nimbus;
        }


        public BlueprintFeature createFlameCurse(string name_prefix, string display_name, string description)
        {
            var fire_belly = library.Get<BlueprintBuff>("7c33de68880aa444bbb916271b653016"); //fire belly

            var buff = Helpers.CreateBuff(name_prefix + "Buff",
                                          display_name,
                                          description,
                                          "",
                                          fire_belly.Icon,
                                          Common.createPrefabLink("f00bbb092bd65a4468e72869b99f1d66"),
                                          Common.createAddEnergyVulnerability(DamageEnergyType.Fire));

            var apply_buff1 = Helpers.CreateActionList(Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(), dispellable: false, duration_seconds: 9));
            var apply_buff2 = Helpers.CreateActionList(Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(), dispellable: false, duration_seconds: 15));
            var apply_buff3 = Helpers.CreateActionList(Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(), dispellable: false, duration_seconds: 21));

            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                buff.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Close,
                                                "Variable",
                                                Helpers.savingThrowNone,
                                                Helpers.CreateRunActions(Common.createRunActionsDependingOnContextValue(Helpers.CreateContextValue(AbilityRankType.StatBonus),
                                                                                                                        apply_buff1, apply_buff2, apply_buff3)),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.OnePlusDivStep,
                                                                           type: AbilityRankType.StatBonus, stepLevel: 8, classes: hex_classes)
                                               );
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);
            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var flame_curse = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            flame_curse.Ranks = 1;
            return flame_curse;
        }

        //waves spirit hexes
        //will need to add fluid magic     
        public BlueprintFeature createCrashingWaves(string name_prefix, string display_name, string description)
        {
            var icon = library.Get<BlueprintAbility>("d8144161e352ca846a73cf90e85bf9ac").Icon;

            var caster_level_increase = Helpers.Create<NewMechanics.ContextIncreaseSpellDescriptorCasterLevel>();
            caster_level_increase.BonusCasterLevel = Helpers.CreateContextValue(AbilityRankType.Default);
            caster_level_increase.Descriptor = SpellDescriptor.Cold;
            var caster_level_increase_config = Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel,
                                                                               progression: ContextRankProgression.OnePlusDivStep,
                                                                               startLevel: 8, max: 2, classes: hex_classes);

            var on_dmg_action = Helpers.CreateActionList(Helpers.Create<ContextActionKnockdownTarget>());
            var crashing_waves1 = Helpers.CreateFeature(name_prefix + "1HexFeature",
                                                        "",
                                                        "",
                                                        "",
                                                        null,
                                                        FeatureGroup.None,
                                                        caster_level_increase,
                                                        caster_level_increase_config,
                                                        Helpers.Create<NewMechanics.ActionOnSpellDamage>(a =>
                                                                                                        {
                                                                                                            a.descriptor = SpellDescriptor.Cold;
                                                                                                            a.save_type = SavingThrowType.Fortitude;
                                                                                                            a.action = on_dmg_action;
                                                                                                        }
                                                                                                        )
                                                        );
            crashing_waves1.HideInCharacterSheetAndLevelUp = true;
            var crashing_waves2 = Helpers.CreateFeature(name_prefix + "2HexFeature",
                                            "",
                                            "",
                                            "",
                                            null,
                                            FeatureGroup.None,
                                            caster_level_increase,
                                            caster_level_increase_config,
                                            Helpers.Create<NewMechanics.ActionOnSpellDamage>(a =>
                                                                                            {
                                                                                                a.descriptor = SpellDescriptor.None;
                                                                                                a.save_type = SavingThrowType.Fortitude;
                                                                                                a.action = on_dmg_action;
                                                                                            }
                                                                                            )
                                            );
            crashing_waves2.HideInCharacterSheetAndLevelUp = true;
            var crashing_waves = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                       display_name,
                                                       description,
                                                       "",
                                                       icon,
                                                       FeatureGroup.None,
                                                       Helpers.CreateAddFeatureOnClassLevel(crashing_waves1, 16, hex_classes, before: true),
                                                       Helpers.CreateAddFeatureOnClassLevel(crashing_waves2, 16, hex_classes)
                                                       );
            return crashing_waves;
        }


        public  BlueprintFeature createBeckoningChill(string name_prefix, string display_name, string description)
        {
            var icy_prison_entangle = library.Get<BlueprintBuff>("c53b286bb06a0544c85ca0f8bcc86950");
            icy_prison_entangle.Stacking = StackingType.Prolong;
            var apply_entangle = Common.createContextActionApplyBuff(icy_prison_entangle, Helpers.CreateContextDuration(1));
            
            var buff = Helpers.CreateBuff(name_prefix + "Buff",
                                          display_name,
                                          description,
                                          "",
                                          icy_prison_entangle.Icon,
                                          Common.createPrefabLink("f00bbb092bd65a4468e72869b99f1d66"),
                                          Helpers.Create<NewMechanics.AddIncomingDamageTriggerOnAttacker>(a =>
                                                                                                          {
                                                                                                              a.on_self = true;
                                                                                                              a.consider_damage_type = true;
                                                                                                              a.energy_types = new DamageEnergyType[] { DamageEnergyType.Cold };
                                                                                                              a.Actions = Helpers.CreateActionList(apply_entangle);
                                                                                                          })
                                          );

            var apply_buff = Common.createContextActionApplyBuff(icy_prison_entangle, Helpers.CreateContextDuration(1, DurationRate.Minutes));
            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                buff.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Close,
                                                Helpers.oneMinuteDuration,
                                                Helpers.savingThrowNone,
                                                Helpers.CreateRunActions(apply_buff)
                                               );
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);
            addWitchHexCooldownScaling(ability, "");

            //addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var beckoning_chill = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            beckoning_chill.Ranks = 1;
            return beckoning_chill;
        }


        public BlueprintFeature createMistsShroud(string name_prefix, string display_name, string description)
        {
            var blur_buff = library.Get<BlueprintBuff>("dd3ad347240624d46a11a092b4dd4674"); 

            var concealement_buffs = new BlueprintBuff[3];
            var actions = new ActionList[3];



            var buff = Helpers.CreateBuff(name_prefix + "Buff",
                                          display_name,
                                          description,
                                          "",
                                          blur_buff.Icon,
                                          blur_buff.FxOnStart);

            GameAction[] remove_action = new GameAction[3];

            for (int i = 0; i < concealement_buffs.Length; i++)
            {
                var on_attack_action = i == 0 ? Helpers.CreateActionList(Helpers.Create<ContextActionRemoveSelf>())
                                              : Helpers.CreateActionList(Helpers.Create<ContextActionRemoveSelf>(), actions[i - 1].Actions[0]);
                concealement_buffs[i] = Helpers.CreateBuff(name_prefix + $"{i + 1}Buff",
                                                display_name,
                                                description + $" (+{i + 1})",
                                                "",
                                                blur_buff.Icon,
                                                null,
                                                Helpers.Create<NewMechanics.AddTargetConcealmentRollTrigger>(a => { a.only_on_miss = true; a.actions = on_attack_action; })
                                                );
                concealement_buffs[i].SetBuffFlags(BuffFlags.HiddenInUi);
                actions[i] = Helpers.CreateActionList(Common.createContextActionApplyBuff(concealement_buffs[i], Helpers.CreateContextDuration(), is_child: true, dispellable: false, is_permanent: true));
                remove_action[i] = Common.createContextActionRemoveBuff(concealement_buffs[i]);
            }


            buff.AddComponents(Helpers.CreateAddFactContextActions(new GameAction[] { Common.createRunActionsDependingOnContextValue(Helpers.CreateContextValue(AbilityRankType.StatBonus), actions) },
                                                                   remove_action),
                                                                   Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.OnePlusDivStep,
                                                                           type: AbilityRankType.StatBonus, stepLevel: 8, classes: hex_classes)
                                                                  );
            var apply_buff = Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(1, DurationRate.Minutes), dispellable: false);
            var hex_ability = Helpers.CreateAbility(name_prefix + "HexAbility",
                                                    display_name,
                                                    description,
                                                    "",
                                                    blur_buff.Icon,
                                                    AbilityType.Supernatural,
                                                    Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                    AbilityRange.Touch,
                                                    "",
                                                    "",
                                                    Helpers.CreateRunActions(apply_buff)
                                                    );

            hex_ability.setMiscAbilityParametersTouchFriendly();

            addWitchHexCooldownScaling(hex_ability, "");

            var mists_shroud = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  hex_ability.Name,
                                                  hex_ability.Description,
                                                  "",
                                                  hex_ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(hex_ability));
            mists_shroud.Ranks = 1;
            return mists_shroud;
        }

        //life spirit hexes
        //curse of suffering from battle spirit
        //will need to add enhanced cures, life link and life sight

        //lore spirit hexes
        //will need to add benefit of wisdom, mental acuity (+1 int per 5 levels)
        //brain drain, confusion curse
        BlueprintFeature createBrainDrain(string name_prefix, string display_name, string description)
        {
            var mind_blank = library.Get<BlueprintAbility>("df2a0ba6b6dcecf429cbb80a56fee5cf");

            var dmg = Helpers.CreateActionDealDamage(DamageEnergyType.Divine, Helpers.CreateContextDiceValue(Kingmaker.RuleSystem.DiceType.D4, Helpers.CreateContextValue(AbilityRankType.DamageBonus)));
            dmg.DamageType.Type = Kingmaker.RuleSystem.Rules.Damage.DamageType.Direct;
            var action = Common.createContextActionSavingThrow(SavingThrowType.Will, Helpers.CreateActionList(Helpers.CreateConditionalSaved(null, dmg)));
            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                mind_blank.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Close,
                                                "",
                                                Helpers.willNegates,
                                                Helpers.CreateRunActions(action),
                                                Helpers.CreateSpellDescriptor(SpellDescriptor.MindAffecting),
                                                Common.createAbilitySpawnFx("cbfe312cb8e63e240a859efaad8e467c", anchor: AbilitySpawnFxAnchor.SelectedTarget),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.Div2,
                                                                                type: AbilityRankType.DamageBonus, min: 1, classes: hex_classes)
                                               );
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);
            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            feature.Ranks = 1;
            return feature;
        }


        BlueprintFeature createConfusionCurse(string name_prefix, string display_name, string description)
        {
            var confused = library.Get<BlueprintBuff>("886c7407dc629dc499b9f1465ff382df");

            var apply_buff = Common.createContextActionApplyBuff(confused, Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.StatBonus)), dispellable: false);
            var action = Common.createContextActionSavingThrow(SavingThrowType.Will, Helpers.CreateActionList(Helpers.CreateConditionalSaved(null, apply_buff)));
            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                confused.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Close,
                                                "",
                                                Helpers.willNegates,
                                                Helpers.CreateRunActions(action),
                                                Helpers.CreateSpellDescriptor(SpellDescriptor.MindAffecting),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.StatBonus, progression: ContextRankProgression.AsIs,
                                                                                type: AbilityRankType.StatBonus, min: 1, stat: StatType.Charisma)
                                               );
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);
            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            feature.Ranks = 1;
            return feature;
        }


        //nature spirit
        //will need to add friend to animals and wilderness stride
        BlueprintFeature createEntanglingCurse(string name_prefix, string display_name, string description)
        {
            var entangle = library.Get<BlueprintBuff>("f7f6330726121cf4b90a6086b05d2e38");

            var apply_buff = Common.createContextActionApplyBuff(entangle, Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.StatBonus)), dispellable: false);
            var action = Common.createContextActionSavingThrow(SavingThrowType.Reflex, Helpers.CreateActionList(Helpers.CreateConditionalSaved(null, apply_buff)));
            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                entangle.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Close,
                                                "",
                                                "Reflex Negates",
                                                Helpers.CreateRunActions(action),
                                                Helpers.CreateSpellDescriptor(SpellDescriptor.MindAffecting),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.StatBonus, progression: ContextRankProgression.AsIs,
                                                                                type: AbilityRankType.StatBonus, min: 1, stat: StatType.Charisma)
                                               );
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);
            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            feature.Ranks = 1;
            return feature;
        }


        BlueprintFeature createErosionCurse(string name_prefix, string display_name, string description)
        {
            var touch_of_slime = library.Get<BlueprintAbility>("1e481e03d9cf1564bae6b4f63aed2d1a");

            var dmg = Helpers.CreateActionDealDamage(DamageEnergyType.Divine, Helpers.CreateContextDiceValue(Kingmaker.RuleSystem.DiceType.D6, Helpers.CreateContextValue(AbilityRankType.DamageBonus)), 
                                                     halfIfSaved: true);
            dmg.DamageType.Type = Kingmaker.RuleSystem.Rules.Damage.DamageType.Direct;
            var action = Common.createContextActionSavingThrow(SavingThrowType.Reflex, Helpers.CreateActionList(dmg));
            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                touch_of_slime.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Close,
                                                "",
                                                Helpers.reflexHalfDamage,
                                                Helpers.CreateRunActions(action),
                                                Helpers.CreateSpellDescriptor(SpellDescriptor.MindAffecting),
                                                Common.createAbilitySpawnFx("524f5d0fecac019469b9e58ce1b8402d", anchor: AbilitySpawnFxAnchor.SelectedTarget),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.Div2,
                                                                                type: AbilityRankType.DamageBonus, min: 1, classes: hex_classes),
                                                Common.createAbilityTargetHasFact(true, library.Get<BlueprintFeature>("fd389783027d63343b4a5634bd81645f"))
                                               );
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);
            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            feature.Ranks = 1;
            return feature;
        }

        //stone spirit hexes

        public BlueprintFeature createStoneStability(string name_prefix, string display_name, string description)
        {
            var improved_trip = library.Get<BlueprintFeature>("0f15c6f70d8fb2b49aa6cc24239cc5fa");
            var greater_trip = library.Get<BlueprintFeature>("4cc71ae82bdd85b40b3cfe6697bb7949");

            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                display_name,
                                                description,
                                                "",
                                                library.Get<BlueprintFeature>("2a6a2f8e492ab174eb3f01acf5b7c90a").Icon, //defensive stance
                                                FeatureGroup.None,
                                                Common.createManeuverDefenseBonus(Kingmaker.RuleSystem.Rules.CombatManeuver.Trip, 4),
                                                Common.createManeuverDefenseBonus(Kingmaker.RuleSystem.Rules.CombatManeuver.BullRush, 4),
                                                Helpers.CreateAddFeatureOnClassLevel(improved_trip, 5, hex_classes),
                                                Helpers.CreateAddFeatureOnClassLevel(greater_trip, 10, hex_classes)
                                                );
            return feature;
        }


        public BlueprintFeature createMetalCurse(string name_prefix, string display_name, string description)
        {
            var magnetic_infusion = library.Get<BlueprintBuff>("07afee46a4533e74bbb2e962768864ad");
            var actions = new ActionList[3];
            for (int i = 0; i < actions.Length; i++)
            {
                var buff = Helpers.CreateBuff(name_prefix + $"{i + 1}HexBuff",
                                              display_name,
                                              description,
                                              "",
                                              magnetic_infusion.Icon,
                                              magnetic_infusion.FxOnStart,
                                              Helpers.Create<ACBonusAgainstWeaponSubcategory>(a => { a.ArmorClassBonus = -(i * 2); a.SubCategory = WeaponSubCategory.Metal; })
                                              );
                actions[i] = Helpers.CreateActionList(Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(i + 1), dispellable: false));
            }

            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                magnetic_infusion.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Close,
                                                "",
                                                Helpers.savingThrowNone,
                                                Helpers.CreateRunActions(Common.createRunActionsDependingOnContextValue(Helpers.CreateContextValue(AbilityRankType.StatBonus),
                                                                                                                        actions)
                                                                        ),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.DivStep,
                                                                                type: AbilityRankType.StatBonus, stepLevel: 8, classes: hex_classes)
                                               );
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);
            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            feature.Ranks = 1;
            return feature;
        }


        public BlueprintFeature createWardOfStone(string name_prefix, string display_name, string description)
        {
            var stoneskin = library.Get<BlueprintBuff>("7aeaf147211349b40bb55c57fec8e28d");

            var dr_buffs = new BlueprintBuff[3];
            var actions = new ActionList[3];

            var dr = Common.createMaterialDR(5, PhysicalDamageMaterial.Adamantite);

            var buff = Helpers.CreateBuff(name_prefix + "Buff",
                                          display_name,
                                          description,
                                          "",
                                          stoneskin.Icon,
                                          stoneskin.FxOnStart);

            GameAction[] remove_action = new GameAction[3];

            for (int i = 0; i < dr_buffs.Length; i++)
            {
                var on_attack_action = i == 0 ? Helpers.CreateActionList(Helpers.Create<ContextActionRemoveSelf>())
                                              : Helpers.CreateActionList(Helpers.Create<ContextActionRemoveSelf>(), actions[i - 1].Actions[0]);
                dr_buffs[i] = Helpers.CreateBuff(name_prefix + $"{i + 1}Buff",
                                                display_name,
                                                description + $" (+{i + 1})",
                                                "",
                                                stoneskin.Icon,
                                                null,
                                                Common.createAddTargetAttackWithWeaponTrigger(on_attack_action,
                                                                                              null,
                                                                                              not_reach: false),
                                                dr
                                                );
                dr_buffs[i].SetBuffFlags(BuffFlags.HiddenInUi);
                actions[i] = Helpers.CreateActionList(Common.createContextActionApplyBuff(dr_buffs[i], Helpers.CreateContextDuration(), is_child: true, dispellable: false, is_permanent: true));
                remove_action[i] = Common.createContextActionRemoveBuff(dr_buffs[i]);
            }


            buff.AddComponents(Helpers.CreateAddFactContextActions(new GameAction[] { Common.createRunActionsDependingOnContextValue(Helpers.CreateContextValue(AbilityRankType.StatBonus), actions) },
                                                                   remove_action),
                                                                   Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.OnePlusDivStep,
                                                                           type: AbilityRankType.StatBonus, stepLevel: 8, classes: hex_classes)
                                                                  );
            var apply_buff = Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(1, DurationRate.Minutes), dispellable: false);
            var hex_ability = Helpers.CreateAbility(name_prefix + "HexAbility",
                                                    display_name,
                                                    description,
                                                    "",
                                                    stoneskin.Icon,
                                                    AbilityType.Supernatural,
                                                    Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                    AbilityRange.Touch,
                                                    "",
                                                    "",
                                                    Helpers.CreateRunActions(apply_buff)
                                                    );

            hex_ability.setMiscAbilityParametersTouchFriendly();

            addWitchHexCooldownScaling(hex_ability, "");

            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  hex_ability.Name,
                                                  hex_ability.Description,
                                                  "",
                                                  hex_ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(hex_ability));
            feature.Ranks = 1;
            return feature;
        }

        //loadstone replace with slow effect
       public  BlueprintFeature createLoadStone(string name_prefix, string display_name, string description)
        {
            var load_stone = library.CopyAndAdd<BlueprintAbility>("f492622e473d34747806bdb39356eb89", name_prefix + "HexAbility", "");

            load_stone.RemoveComponents<SpellListComponent>();
            load_stone.RemoveComponents<SpellComponent>();
            load_stone.RemoveComponents<AbilityTargetsAround>();
            load_stone.setMiscAbilityParametersSingleTargetRangedHarmful();
            load_stone.AvailableMetamagic = 0;
            load_stone.SpellResistance = false;
            load_stone.SetNameDescription(display_name, description);
            addWitchHexCooldownScaling(load_stone, "");


            addToAmplifyHex(load_stone);
            //addToSplitHex(load_stone, true);
            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  load_stone.Name,
                                                  load_stone.Description,
                                                  "",
                                                  load_stone.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(load_stone));
            feature.Ranks = 1;
            return feature;
        }


        //wind spirit hexes
        public BlueprintFeature createAirBarrier(string name_prefix, string display_name, string description)
        {
            var mage_armor = library.Get<BlueprintAbility>("9e1ad5d6f87d19e4d8883d63a6e35568");
            var buff = Helpers.CreateBuff(name_prefix + "HexBuff",
                                          display_name,
                                          description,
                                          "",
                                          mage_armor.Icon,
                                          null,
                                          Helpers.CreateAddContextStatBonus(StatType.AC, ModifierDescriptor.Armor, rankType: AbilityRankType.Default, multiplier: 2),
                                          Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.StartPlusDivStep,
                                                                          classes: hex_classes, stepLevel: 4, startLevel: -5, min: 2)
                                         );
            var buff2 = Helpers.CreateBuff(name_prefix + "Hex2Buff",
                              display_name,
                              description,
                              "",
                              buff.Icon,
                              null,
                              Helpers.Create<AddConcealment>(c => { c.CheckWeaponRangeType = true;
                                                                    c.RangeType = AttackTypeAttackBonus.WeaponRangeType.Ranged;
                                                                    c.Concealment = Concealment.Total;
                                                                    c.Descriptor = ConcealmentDescriptor.Fog;
                                                                  }
                                                            )
                             );
            buff2.SetBuffFlags(BuffFlags.HiddenInUi);

            var apply_buff = Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(1, DurationRate.Hours), dispellable: false);
            var apply_buff2 = Common.createContextActionApplyBuff(buff2, Helpers.CreateContextDuration(1, DurationRate.Hours), dispellable: false);
            var resource = Helpers.CreateAbilityResource(name_prefix + "HexResource", "", "", "", null);
            resource.SetIncreasedByLevel(0, 1, hex_classes);

            var ability = Helpers.CreateAbility(name_prefix + "HexAbility",
                                                display_name,
                                                description,
                                                "",
                                                buff.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Personal,
                                                "One hour",
                                                "",
                                                mage_armor.GetComponent<AbilitySpawnFx>(),
                                                Helpers.CreateRunActions(Common.createRunActionsDependingOnContextValue(Helpers.CreateContextValue(AbilityRankType.StatBonus),
                                                                                                                        Helpers.CreateActionList(apply_buff),
                                                                                                                        Helpers.CreateActionList(apply_buff, apply_buff2)
                                                                                                                        )
                                                                        ),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, classes: hex_classes,
                                                                                type: AbilityRankType.StatBonus, progression: ContextRankProgression.OnePlusDivStep,
                                                                                stepLevel: 13),
                                                Helpers.CreateResourceLogic(resource)
                                                );
            ability.setMiscAbilityParametersSelfOnly();
            var feature = Common.AbilityToFeature(ability);
            feature.AddComponent(Helpers.CreateAddAbilityResource(resource));

            return feature;
        }


        public BlueprintFeature createVortexSpells(string name_prefix, string display_name, string description)
        {
            var icon = library.Get<BlueprintFeature>("f2fa7541f18b8af4896fbaf9f2a21dfe").Icon; //cyclone form infusion

            var staggered = library.Get<BlueprintBuff>("df3950af5a783bd4d91ab73eb8fa0fd3");
            var apply_staggered1 = Common.createContextActionApplyBuff(staggered, Helpers.CreateContextDuration(1), dispellable: false);
            var apply_staggered1d4 = Common.createContextActionApplyBuff(staggered, Helpers.CreateContextDuration(0, diceType: Kingmaker.RuleSystem.DiceType.D4, diceCount: 1), dispellable: false);
            var action = Common.createRunActionsDependingOnContextValue(Helpers.CreateContextValue(AbilityRankType.Default),
                                                                        Helpers.CreateActionList(apply_staggered1),
                                                                        Helpers.CreateActionList(apply_staggered1d4)
                                                                        );
            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                display_name,
                                                description,
                                                "",
                                                icon,
                                                FeatureGroup.None,
                                                Helpers.Create<NewMechanics.ActionOnSpellDamage>(a => { a.only_critical = true; a.action = Helpers.CreateActionList(action); }),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, classes: hex_classes,
                                                                                progression: ContextRankProgression.OnePlusDivStep, stepLevel: 11)
                                               );
            return feature;
        }


        public BlueprintFeature createSparklingAura(string name_prefix, string display_name, string description)
        {
            var buff = library.CopyAndAdd<BlueprintBuff>("cc383a9eaae4d2b45a925d442b367b54", name_prefix + "Buff", ""); //faery fire           
            buff.SetNameDescriptionIcon(display_name, description, LoadIcons.Image2Sprite.Create(@"AbilityIcons/SparklingAura.png"));
            var dmg = Helpers.CreateActionDealDamage(DamageEnergyType.Electricity,
                                                     Helpers.CreateContextDiceValue(Kingmaker.RuleSystem.DiceType.Zero, bonus: Helpers.CreateContextValue(AbilityRankType.DamageBonus)),
                                                     IgnoreCritical: true);
            var on_hit = Helpers.Create<NewMechanics.TargetWeaponSubCategoryAttackTrigger>(w =>
            {
                w.ActionOnSelf = Helpers.CreateActionList(dmg);
                w.ActionsOnAttacker = Helpers.CreateActionList();
                w.SubCategory = WeaponSubCategory.Metal;
            });
            buff.AddComponents(on_hit,
                               Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.StatBonus, progression: ContextRankProgression.AsIs,
                                                                           type: AbilityRankType.DamageBonus, stat: StatType.Charisma)
                               );
            var apply_buff = Common.createContextActionApplyBuff(buff, Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.StatBonus)), dispellable: false);
            var ability = Helpers.CreateAbility(name_prefix + "Ability",
                                                display_name,
                                                description,
                                                "",
                                                buff.Icon,
                                                AbilityType.Supernatural,
                                                Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                AbilityRange.Close,
                                                "1 round / 2 levels",
                                                Helpers.savingThrowNone,
                                                Helpers.CreateRunActions(apply_buff),
                                                Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.Div2,
                                                                           type: AbilityRankType.StatBonus, min: 1, classes: hex_classes)
                                               );
            ability.setMiscAbilityParametersSingleTargetRangedHarmful(test_mode);
            addWitchHexCooldownScaling(ability, "");

            addToAmplifyHex(ability);
            //addToSplitHex(ability, true);
            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  ability.Name,
                                                  ability.Description,
                                                  "",
                                                  ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(ability));
            feature.Ranks = 1;
            return feature;
        }


        public BlueprintFeature createWindWard(string name_prefix, string display_name, string description)
        {
            var buff1 = library.Get<BlueprintBuff>("49786ccc94a5ee848a5637b4145b2092");//chameleon stride
            var buff2 = library.CopyAndAdd<BlueprintBuff>("49786ccc94a5ee848a5637b4145b2092", name_prefix + "2HexBuff", "");
            buff2.ReplaceComponent<AddConcealment>(a => a.Concealment = Concealment.Total);


            var action1 = Helpers.CreateActionList(Common.createContextActionApplyBuff(buff1, Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.Default), DurationRate.Rounds), dispellable: false));
            var action2 = Helpers.CreateActionList(Common.createContextActionApplyBuff(buff1, Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.Default), DurationRate.Minutes), dispellable: false));
            var action3 = Helpers.CreateActionList(Common.createContextActionApplyBuff(buff2, Helpers.CreateContextDuration(Helpers.CreateContextValue(AbilityRankType.Default), DurationRate.Minutes), dispellable: false));

            var hex_ability = Helpers.CreateAbility(name_prefix + "HexAbility",
                                                    display_name,
                                                    description,
                                                    "",
                                                    buff1.Icon,
                                                    AbilityType.Supernatural,
                                                    Kingmaker.UnitLogic.Commands.Base.UnitCommand.CommandType.Standard,
                                                    AbilityRange.Touch,
                                                    "",
                                                    "",
                                                    Helpers.CreateRunActions(Common.createRunActionsDependingOnContextValue(Helpers.CreateContextValue(AbilityRankType.StatBonus),
                                                                                                                            action1,
                                                                                                                            action2,
                                                                                                                            action3)
                                                                             ),
                                                    Helpers.CreateContextRankConfig(baseValueType: ContextRankBaseValueType.ClassLevel, progression: ContextRankProgression.OnePlusDivStep,
                                                                           type: AbilityRankType.StatBonus, stepLevel: 8, classes: hex_classes)
                                                    );

            hex_ability.setMiscAbilityParametersTouchFriendly();

            addWitchHexCooldownScaling(hex_ability, "");

            var feature = Helpers.CreateFeature(name_prefix + "HexFeature",
                                                  hex_ability.Name,
                                                  hex_ability.Description,
                                                  "",
                                                  hex_ability.Icon,
                                                  FeatureGroup.None,
                                                  Helpers.CreateAddFact(hex_ability));
            feature.Ranks = 1;
            return feature;
        }
    }
}