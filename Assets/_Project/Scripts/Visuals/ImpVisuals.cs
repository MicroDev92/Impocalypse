namespace _Project.Scripts.Visuals
{
    using UnityEngine;

    public class ImpVisuals : MonoBehaviour
    {
        public Sprite redImp;
        public Sprite greenImp;
        public Sprite blueImp;
        public Sprite purpleImp;

        public Sprite GetSprite(string color)
        {
            return color.ToLower() switch
            {
                "red" => redImp,
                "green" => greenImp,
                "blue" => blueImp,
                "purple" => purpleImp,
                _ => redImp
            };
        }
    }

}