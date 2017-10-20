namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using JetBrains.Annotations;

    public interface ITracer
    {
        void TraceError([CanBeNull] string value);
        void WriteLine([CanBeNull] string value);
    }

    public static class TracerExtensions
    {
        public static void TraceError([NotNull] this ITracer tracer, [NotNull] string format, [NotNull, ItemNotNull] params object[] args)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);
            tracer.TraceError(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void WriteLine([NotNull] this ITracer tracer, [NotNull] string format, [NotNull, ItemNotNull] params object[] args)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(format != null);
            Contract.Requires(args != null);
            tracer.WriteLine(string.Format(CultureInfo.CurrentCulture, format, args));
        }

        public static void TraceError([NotNull] this ITracer tracer, [NotNull] Exception ex)
        {
            Contract.Requires(tracer != null);
            Contract.Requires(ex != null);

            tracer.WriteLine(ex.ToString());
        }
    }
}