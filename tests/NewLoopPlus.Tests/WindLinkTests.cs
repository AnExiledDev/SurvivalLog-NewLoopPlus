using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NewLoopPlus.Core;
using Xunit;

// Stands in for Wind Power's own class, with the same namespace, name and Add signature (Wind Power 0.9.3).
namespace WindPower
{
    public static class WindBoosts
    {
        public static readonly Dictionary<string, Func<float>> Added = new();

        public static void Add(string source, Func<float> multiplier) => Added[source] = multiplier;
    }
}

namespace NewLoopPlus.Tests
{
    /// <summary>The wind talents reach Wind Power's turbines when it is installed, and stay out of the game when it is not.</summary>
    public class WindLinkTests
    {
        private static readonly int[] WindTalents = { 5135, 5136 };

        private static TalentFile DefaultFile() => TalentFile.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "talents.default.json")));

        [Fact]
        public void With_Wind_Power_loaded_the_talents_reach_its_turbines_as_they_are_bought()
        {
            var share = 0.15;

            Assert.True(WindLink.Add(new[] { typeof(WindLinkTests).Assembly }, "New Loop Plus", () => WindLink.Multiplier(share)));

            // Wind Power calls the function each time it reads the multipliers, so a level bought mid-run reaches it.
            Assert.Equal(1.15f, WindPower.WindBoosts.Added["New Loop Plus"](), 5);
            share = 0.95;
            Assert.Equal(1.95f, WindPower.WindBoosts.Added["New Loop Plus"](), 5);
        }

        [Fact]
        public void Without_Wind_Power_nothing_is_added_and_nothing_breaks()
        {
            Assert.False(WindLink.Add(new[] { typeof(FactAttribute).Assembly, typeof(object).Assembly }, "New Loop Plus", () => 2f));
            Assert.False(WindLink.Add(Array.Empty<Assembly>(), "New Loop Plus", () => 2f));
        }

        [Theory]
        [InlineData(0.0, 1f)]
        [InlineData(0.45, 1.45f)]
        [InlineData(-0.5, 1f)]
        [InlineData(double.NaN, 1f)]
        public void No_wind_talent_ever_makes_the_turbines_weaker(double share, float multiplier)
        {
            Assert.Equal(multiplier, WindLink.Multiplier(share), 5);
        }

        [Fact]
        public void Without_Wind_Power_only_the_wind_talents_are_left_out()
        {
            var file = DefaultFile();
            var without = WindLink.WithoutWind(file);

            Assert.All(WindTalents, id => Assert.Contains(file.Talents, talent => talent.Id == id));
            Assert.Equal(file.Talents.Select(talent => talent.Id).Except(WindTalents), without.Talents.Select(talent => talent.Id));
        }

        [Fact]
        public void The_wind_talents_give_more_at_every_level_and_cost_more_too()
        {
            var file = DefaultFile();

            foreach (var id in WindTalents)
            {
                var talent = file.Talents.Single(entry => entry.Id == id);
                var give = Assert.Single(talent.Gives);
                var costs = talent.Levels!.Select(level => level.Cost ?? 0).ToList();
                var values = give.Levels.Select(fields => Assert.IsType<FieldValue.Number>(Assert.Single(fields).Value).Value).ToList();

                Assert.Equal(TalentPlan.NlpPrefix + NlpEffect.WindPower, give.Effect);
                Assert.Equal(talent.Levels!.Count, give.Levels.Count);
                Assert.True(values[0] > 0, $"talent {id}");
                Assert.All(values.Zip(values.Skip(1)), pair => Assert.True(pair.Second > pair.First, $"talent {id}"));
                Assert.All(costs.Zip(costs.Skip(1)), pair => Assert.True(pair.Second > pair.First, $"talent {id}"));
            }
        }
    }
}
