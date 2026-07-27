using System;
using System.Text.RegularExpressions;
using NutriMind.Core.Utilities;
using NUnit.Framework;

namespace NutriMind.Tests.EditMode
{
    public sealed class DeterministicMockIdGeneratorTests
    {
        private static readonly Regex UuidPattern = new Regex(
            @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled);

        [Test]
        public void NewUuid_UsesStableFormatAndSequence()
        {
            var ids = new DeterministicMockIdGenerator(1);

            string first = ids.NewUuid();
            string second = ids.NewUuid();

            Assert.That(first, Is.EqualTo("00000000-0000-4000-8000-000000000001"));
            Assert.That(second, Is.EqualTo("00000000-0000-4000-8000-000000000002"));
            Assert.That(UuidPattern.IsMatch(first), Is.True);
            Assert.That(UuidPattern.IsMatch(second), Is.True);
            Assert.That(Guid.TryParse(first, out _), Is.True);
        }

        [Test]
        public void Reset_RestartsSequence()
        {
            var ids = new DeterministicMockIdGenerator(5);
            Assert.That(ids.NewUuid(), Is.EqualTo("00000000-0000-4000-8000-000000000005"));

            ids.Reset(1);
            Assert.That(ids.NewUuid(), Is.EqualTo("00000000-0000-4000-8000-000000000001"));
        }
    }
}
