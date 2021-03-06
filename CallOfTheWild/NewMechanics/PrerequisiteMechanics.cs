﻿using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallOfTheWild.PrerequisiteMechanics
{

    [AllowMultipleComponents]
    public class PrerequsiteOrAlternative : Prerequisite
    {        
        public Prerequisite base_prerequsite, alternative_prerequsite;

        public override bool Check(
          FeatureSelectionState selectionState,
          UnitDescriptor unit,
          LevelUpState state)
        {
            return this.base_prerequsite.Check(selectionState, unit, state) || this.alternative_prerequsite.Check(selectionState, unit, state);
        }

        public override string GetUIText()
        {
            return base_prerequsite.GetUIText() + " or " + this.alternative_prerequsite.GetUIText();
        }
    }


    [AllowMultipleComponents]
    public class PrerequisiteNoFeatures : Prerequisite
    {
        public BlueprintFeature[] Features;

        public override bool Check(
          FeatureSelectionState selectionState,
          UnitDescriptor unit,
          LevelUpState state)
        {
            foreach (var f in Features)
            {
                if (unit.Logic.HasFact(f))
                {
                    return false;
                }
                if (unit.Progression.Features.HasFact(f))
                {
                    return false;
                }
            }
            return true;
        }

        public override string GetUIText()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(string.Format("{0}:\n", (object)UIStrings.Instance.Tooltips.NoFeature));
            for (int index = 0; index < this.Features.Length; ++index)
            {
                stringBuilder.Append(this.Features[index].Name);
                if (index < this.Features.Length - 1)
                    stringBuilder.Append("\n");
            }
            return stringBuilder.ToString();
        }
    }
}
