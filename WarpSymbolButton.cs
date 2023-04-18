using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Menu
{
    public class WarpSymbolButton : SymbolButton
    {
        public Color color = new Color(0.7f, 0.7f, 0.7f);
        public WarpSymbolButton(Menu menu, MenuObject owner, string symbolName, string singalText, Vector2 pos) : base(menu, owner, symbolName, singalText, pos)
        {

        }

        public override Color MyColor(float timeStacker)
        {
            if (!buttonBehav.greyedOut)
            {
                float num = Mathf.Lerp(buttonBehav.lastCol, buttonBehav.col, timeStacker);
                num = Mathf.Max(num, Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
                Color from = Color.Lerp(Color.Lerp(Menu.MenuColor(Menu.MenuColors.DarkGrey).rgb, color, 0.7f), color, num);
                return Color.Lerp(from, Menu.MenuColor(Menu.MenuColors.Black).rgb, black);
            }
            if (maintainOutlineColorWhenGreyedOut)
            {
                return Menu.MenuRGB(Menu.MenuColors.DarkGrey);
            }
            return HSLColor.Lerp(Menu.MenuColor(Menu.MenuColors.VeryDarkGrey), Menu.MenuColor(Menu.MenuColors.Black), black).rgb;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            symbolSprite.color = MyColor(timeStacker);
        }
    }
}
