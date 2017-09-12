namespace tomenglertde.ProjectConfigurationManager.Model
{
    using System;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Threading;

    using JetBrains.Annotations;

    [Export]
    public sealed class PerformanceTracer
    {
        [NotNull]
        private readonly ITracer _tracer;
        private int _index;

        [ImportingConstructor]
        public PerformanceTracer([NotNull] ITracer tracer)
        {
            Contract.Requires(tracer != null);

            _tracer = tracer;
        }

        [NotNull]
        public IDisposable Start([NotNull] string message)
        {
            Contract.Requires(message != null);
            Contract.Ensures(Contract.Result<IDisposable>() != null);

            return new Tracer(_tracer, Interlocked.Increment(ref _index), message);
        }

        private sealed class Tracer : IDisposable
        {
            [NotNull]
            private readonly ITracer _tracer;
            private readonly int _index;
            [NotNull]
            private readonly string _message;
            [NotNull]
            private readonly Stopwatch _stopwatch = new Stopwatch();

            public Tracer([NotNull] ITracer tracer, int index, [NotNull] string message)
            {
                Contract.Requires(tracer != null);
                Contract.Requires(message != null);

                _tracer = tracer;
                _index = index;
                _message = message;

                _stopwatch.Start();

                _tracer.WriteLine(">>> {0}: {1} @{2}", _index, _message, DateTime.Now.ToString("HH:mm:ss.f", CultureInfo.InvariantCulture));
            }


            public void Dispose()
            {
                _tracer.WriteLine("<<< {0}: {1} {2}", _index, _message, _stopwatch.Elapsed);
            }

            [ContractInvariantMethod]
            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            [Conditional("CONTRACTS_FULL")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_tracer != null);
                Contract.Invariant(_message != null);
                Contract.Invariant(_stopwatch != null);
            }
        }

        [ContractInvariantMethod]
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        [Conditional("CONTRACTS_FULL")]
        private void ObjectInvariant()
        {
            Contract.Invariant(_tracer != null);
        }
    }
}
