using System;

namespace KitapKosesi.Models
{
    public class HataViewModel
    {
        public string IstekKimligi { get; set; }
        public string HataMesaji { get; set; }

        public bool IstekKimligiGoster => !string.IsNullOrEmpty(IstekKimligi);

        public string RequestId { get; internal set; }
    }
} 