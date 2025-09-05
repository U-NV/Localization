using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace U0UGames.Localization.UI
{    
    [RequireComponent(typeof(Image))]
    public class LocalizeImage:LocalizeSprite
    {
        private Image _target;
        private Image Target
        {
            get
            {
                if (!_target)
                {
                    _target = GetComponent<Image>();
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