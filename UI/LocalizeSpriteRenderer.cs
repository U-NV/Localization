using UnityEngine;

namespace U0UGames.Localization.UI
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class LocalizeSpriteRenderer:LocalizeSprite
    {
        private SpriteRenderer _target;
        private SpriteRenderer Target
        {
            get
            {
                if (!_target)
                {
                    _target = GetComponent<SpriteRenderer>();
                }
                return _target;
            }
            set => _target = value;
        }
        
        public Color color
        {
            get => Target.color;
            set => Target.color = value;
        }
        
        protected override void SetSprite(Sprite sprite)
        {
            base.SetSprite(sprite);
            Target.sprite = sprite;
        }
    }
}