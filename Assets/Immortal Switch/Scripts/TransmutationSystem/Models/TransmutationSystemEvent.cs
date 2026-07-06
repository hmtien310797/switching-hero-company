using System.Numerics;

namespace Immortal_Switch.Scripts.TransmutationSystem.Models
{
    public class TransmutationSystemChanged
    {
        /// <summary>
        /// du lieu cua user
        /// </summary>
        public TransmutationSystemData Data { get; set; }

        /// <summary>
        /// target exp de len cap moi
        /// </summary>
        public BigInteger TargetExp { get; set; }

        /// <summary>
        /// progress hien tai
        /// </summary>
        public float Progress { get; set; }
    }
}