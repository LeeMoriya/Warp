using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RWCustom;
using Menu;


public class WarpButton : ButtonTemplate
{
    public Color color;
    public Color defaultColor;
    public MenuLabel menuLabel;
    public RoundedRect roundedRect;
    public RoundedRect selectRect;
    public string signalText;

    public WarpButton(Menu.Menu menu, MenuObject owner, string displayText, string singalText, Vector2 pos, Vector2 size, Color color) : base(menu, owner, pos, size)
    {
        this.color = color;
        this.defaultColor = color;
        this.signalText = singalText;
        this.roundedRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, true);
        this.subObjects.Add(this.roundedRect);
        this.selectRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, false);
        this.subObjects.Add(this.selectRect);
        this.menuLabel = new MenuLabel(menu, this, displayText, new Vector2(0f, 0f), size, false);
        this.subObjects.Add(this.menuLabel);
    }

    public void SetSize(Vector2 newSize)
    {
        this.size = newSize;
        this.roundedRect.size = this.size;
        this.selectRect.size = this.size;
        this.menuLabel.size = this.size;
    }

    public override void Update()
    {
        base.Update();
        this.buttonBehav.Update();
        this.roundedRect.fillAlpha = Mathf.Lerp(0.3f, 0.6f, this.buttonBehav.col);
        this.roundedRect.addSize = new Vector2(10f, 6f) * (this.buttonBehav.sizeBump + 0.5f * Mathf.Sin(this.buttonBehav.extraSizeBump * 3.14159274f)) * ((!this.buttonBehav.clicked) ? 1f : 0f);
        this.selectRect.addSize = new Vector2(2f, -2f) * (this.buttonBehav.sizeBump + 0.5f * Mathf.Sin(this.buttonBehav.extraSizeBump * 3.14159274f)) * ((!this.buttonBehav.clicked) ? 1f : 0f);
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        this.menuLabel.label.color = this.MyColor(timeStacker);
        Color color = Color.Lerp(Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White), Mathf.Lerp(this.buttonBehav.lastFlash, this.buttonBehav.flash, timeStacker));
        for (int i = 0; i < 9; i++)
        {
            this.roundedRect.sprites[i].color = color;
        }
        float num = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(this.buttonBehav.lastSin, this.buttonBehav.sin, timeStacker) / 30f * 3.14159274f * 2f);
        num *= this.buttonBehav.sizeBump;
        for (int j = 0; j < 8; j++)
        {
            this.selectRect.sprites[j].color = this.MyColor(timeStacker);
            this.selectRect.sprites[j].alpha = num;
        }
    }

    public override Color MyColor(float timeStacker)
    {
        if (this.buttonBehav.greyedOut)
        {
            return Color.Lerp(this.color, new Color(0.05f,0.05f,0.05f), this.black);
        }
        float num = Mathf.Lerp(this.buttonBehav.lastCol, this.buttonBehav.col, timeStacker);
        num = Mathf.Max(num, Mathf.Lerp(this.buttonBehav.lastFlash, this.buttonBehav.flash, timeStacker));
        Color from = Color.Lerp(this.color, Menu.Menu.MenuColor(Menu.Menu.MenuColors.White).rgb, num);
        return Color.Lerp(from, new Color(0.05f, 0.05f, 0.05f), this.black);
    }

    public override void Clicked()
    {
        this.Singal(this, this.signalText);
    }

}

