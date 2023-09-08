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
        roundedRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, true);
        subObjects.Add(roundedRect);
        selectRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, false);
        subObjects.Add(selectRect);
        menuLabel = new MenuLabel(menu, this, displayText, new Vector2(0f, 0f), size, false);
        subObjects.Add(menuLabel);
    }

    public void SetSize(Vector2 newSize)
    {
        size = newSize;
        roundedRect.size = size;
        selectRect.size = size;
        menuLabel.size = size;
    }

    public override void Update()
    {
        base.Update();
        buttonBehav.Update();
        roundedRect.fillAlpha = Mathf.Lerp(0.3f, 0.6f, buttonBehav.col);
        roundedRect.addSize = new Vector2(10f, 6f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * 3.14159274f)) * ((!buttonBehav.clicked) ? 1f : 0f);
        selectRect.addSize = new Vector2(2f, -2f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * 3.14159274f)) * ((!buttonBehav.clicked) ? 1f : 0f);
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        menuLabel.label.color = MyColor(timeStacker);
        Color color = Color.Lerp(Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White), Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
        for (int i = 0; i < 9; i++)
        {
            roundedRect.sprites[i].color = color;
        }
        float num = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 30f * 3.14159274f * 2f);
        num *= buttonBehav.sizeBump;
        for (int j = 0; j < 8; j++)
        {
            selectRect.sprites[j].color = MyColor(timeStacker);
            selectRect.sprites[j].alpha = num;
        }
    }

    public override Color MyColor(float timeStacker)
    {
        if (buttonBehav.greyedOut)
        {
            return Color.Lerp(color, new Color(0.05f,0.05f,0.05f), black);
        }
        float num = Mathf.Lerp(buttonBehav.lastCol, buttonBehav.col, timeStacker);
        num = Mathf.Max(num, Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
        Color from = Color.Lerp(color, Menu.Menu.MenuColor(Menu.Menu.MenuColors.White).rgb, num);
        return Color.Lerp(from, new Color(0.05f, 0.05f, 0.05f), black);
    }

    public override void Clicked()
    {
        Singal(this, signalText);
    }

}

