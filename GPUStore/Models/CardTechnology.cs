namespace GPUStore.Models
{
    public class CardTechnology
    {
        public int VideoCardId { get; set; }
        public virtual VideoCard VideoCard { get; set; }

        public int TechnologyId { get; set; }
        public virtual Technology Technology { get; set; }
    }
}
