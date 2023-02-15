using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

namespace Rebus.Diagnostics.Tests.Outgoing
{
    internal class MeterObserver
    {
        private readonly List<Instrument> _publishedInstruments;
        public MeterObserver()
        {
            _publishedInstruments = new List<Instrument>();

            var meterListener = new MeterListener();
            meterListener.InstrumentPublished += (instrument, _) => _publishedInstruments.Add(instrument);
            meterListener.Start();
        }

        public bool InstrumentCalled(string name)
            => _publishedInstruments.Any(p => p.Name == name);
    }
}