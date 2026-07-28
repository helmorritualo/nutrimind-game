using System.Collections.Generic;

namespace NutriMind.Gameplay.Runtime
{
    public static class EventSequenceValidator
    {
        public static bool CanConfirm(IReadOnlyList<string> slotAssignments)
        {
            if (slotAssignments == null || slotAssignments.Count != 3)
            {
                return false;
            }

            foreach (string assignment in slotAssignments)
            {
                if (string.IsNullOrEmpty(assignment))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsCorrectOrder(IReadOnlyList<string> slotAssignments)
        {
            if (!CanConfirm(slotAssignments))
            {
                return false;
            }

            for (int i = 0; i < MissionContentIds.EventSequenceCardIds.Length; i++)
            {
                if (!string.Equals(slotAssignments[i], MissionContentIds.EventSequenceCardIds[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
