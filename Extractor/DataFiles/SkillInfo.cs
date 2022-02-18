using System;
using System.Diagnostics;

namespace Extractor {
    public enum Skill {
        General,

        Farming,
        Mining,
        Woodcutting,
        Gathering,

        Forging,
        Carpentry,
        Cooking,
        Tailoring
    }

    public struct SkillInfo {
        public int Id { get; set; }

        public int Overall { get; set; }
        public int Planting { get; set; }
        public int Mining { get; set; }
        public int Woodcutting { get; set; }
        public int Gathering { get; set; }
        public int Forging { get; set; }
        public int Carpentry { get; set; }
        public int Cooking { get; set; }
        public int Tailoring { get; set; }

        public static SkillInfo[] Load(SeanArchive.Item data) {
            var contents = new SeanDatabase(data.Contents);

            var items = new SkillInfo[36]; // ignore contents.ItemCount
            for(int i = 0; i < 35; i++) {
                Debug.Assert(contents.Items[i, 0] == i);

                items[i] = new SkillInfo {
                    Id = contents.Items[i, 0],
                    Overall = contents.Items[i, 1],
                    Planting = contents.Items[i, 2],
                    Mining = contents.Items[i, 3],
                    Woodcutting = contents.Items[i, 4],
                    Gathering = contents.Items[i, 5],
                    Forging = contents.Items[i, 6],
                    Carpentry = contents.Items[i, 7],
                    Cooking = contents.Items[i, 8],
                    Tailoring = contents.Items[i, 9]
                };
            }

            return items;
        }

        public int GetExp(Skill skill) {
            return skill switch {
                Skill.General => Overall,
                Skill.Farming => Planting,
                Skill.Mining => Mining,
                Skill.Woodcutting => Woodcutting,
                Skill.Gathering => Gathering,
                Skill.Forging => Forging,
                Skill.Carpentry => Carpentry,
                Skill.Cooking => Cooking,
                Skill.Tailoring => Tailoring,
                _ => throw new ArgumentOutOfRangeException(nameof(skill), skill, null)
            };
        }
    }
}