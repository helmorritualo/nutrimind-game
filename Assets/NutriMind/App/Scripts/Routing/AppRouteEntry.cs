using System;

namespace NutriMind.App.Routing
{
    /// <summary>
    /// One stack entry: route id plus typed context.
    /// </summary>
    public readonly struct AppRouteEntry : IEquatable<AppRouteEntry>
    {
        public AppRouteEntry(AppRouteId routeId, AppRouteContext context = null)
        {
            RouteId = routeId;
            Context = context ?? AppRouteContext.Empty;
        }

        public AppRouteId RouteId { get; }
        public AppRouteContext Context { get; }

        public bool Equals(AppRouteEntry other)
        {
            return RouteId == other.RouteId
                   && ReferenceEquals(Context, other.Context);
        }

        public override bool Equals(object obj)
        {
            return obj is AppRouteEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)RouteId * 397) ^ (Context != null ? Context.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return RouteId.ToString();
        }
    }
}
