using System;
using System.Drawing;
using System.Windows.Forms;

sealed partial class MainForm {
    readonly Panel viewport=new Panel();
    readonly TableLayoutPanel layout=new TableLayoutPanel(), cardGrid=new TableLayoutPanel();
    readonly CheckBox startupCheck=new CheckBox();
    readonly Label startupHint=new Label();
    bool sizingLayout;

    static TableLayoutPanel Table(int columns,int rows) {
        var table=new TableLayoutPanel {ColumnCount=columns,RowCount=rows,Dock=DockStyle.Fill,Margin=Padding.Empty,Padding=Padding.Empty};
        if(rows==1) table.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        if(columns==1) table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        return table;
    }
    static Label LabelText(string text,float size,FontStyle weight) { return new Label {Text=text,Dock=DockStyle.Fill,Margin=Padding.Empty,Font=new Font("Microsoft YaHei UI",size,weight,GraphicsUnit.Pixel),ForeColor=weight==FontStyle.Bold?Ink:Muted,TextAlign=ContentAlignment.MiddleLeft}; }
    static void ButtonStyle(Button b) { b.FlatStyle=FlatStyle.Flat; b.FlatAppearance.BorderColor=Color.FromArgb(210,221,213); b.Dock=DockStyle.Fill; b.Margin=new Padding(4); }
    void BuildResponsiveLayout() {
        viewport.Dock=DockStyle.Fill; viewport.AutoScroll=true; viewport.Padding=new Padding(24,20,24,20); Controls.Add(viewport);
        layout.ColumnCount=1; layout.RowCount=6; layout.Dock=DockStyle.Top; layout.Margin=Padding.Empty;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,126));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,206));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,76));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,68));
        viewport.Controls.Add(layout);

        var header=Table(1,3);
        header.RowStyles.Add(new RowStyle(SizeType.Absolute,28)); header.RowStyles.Add(new RowStyle(SizeType.Absolute,60)); header.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var brand=LabelText("MOUSE STUDIO   /   指针工坊",15,FontStyle.Bold); brand.ForeColor=Green; header.Controls.Add(brand,0,0);
        header.Controls.Add(LabelText("让每一次移动，都有你的风格。",29,FontStyle.Bold),0,1);
        header.Controls.Add(LabelText("40 款指针  ·  独立配色  ·  自动记忆  ·  随时恢复",14,FontStyle.Regular),0,2);
        layout.Controls.Add(header,0,0);

        var pager=Table(4,1);
        pager.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); pager.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,86)); pager.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,116)); pager.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,86));
        pager.Controls.Add(LabelText("01   选择你的指针",18,FontStyle.Bold),0,0);
        previousPage.Text="上一页"; nextPage.Text="下一页";
        ButtonStyle(previousPage); ButtonStyle(nextPage);
        pageLabel.Dock=DockStyle.Fill; pageLabel.TextAlign=ContentAlignment.MiddleCenter;
        pager.Controls.Add(previousPage,1,0); pager.Controls.Add(pageLabel,2,0); pager.Controls.Add(nextPage,3,0);
        previousPage.Click+=delegate { SetPage(page-1); }; nextPage.Click+=delegate { SetPage(page+1); };
        layout.Controls.Add(pager,0,1);

        cardGrid.Dock=DockStyle.Fill; cardGrid.ColumnCount=5; cardGrid.RowCount=2; cardGrid.Margin=new Padding(-5,4,-5,12);
        for(int i=0;i<5;i++) cardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,20));
        for(int i=0;i<2;i++) cardGrid.RowStyles.Add(new RowStyle(SizeType.Percent,50));
        layout.Controls.Add(cardGrid,0,2);
        for(int i=0;i<Art.Names.Length;i++) {
            var card=new StyleCard(i) {Dock=DockStyle.Fill,Margin=new Padding(5),Visible=false};
            card.Click+=delegate(object sender,EventArgs e) { selected=((StyleCard)sender).Index; ApplyStyle(); };
            card.ColorRequested+=delegate(object sender,EventArgs e) { OpenPalette((StyleCard)sender); };
            tips.SetToolTip(card.ColorButton,"为「"+Art.Names[i]+"」选择颜色"); cards.Add(card);
        }
        SetPage(0);

        var adjustments=Table(3,1); adjustments.BackColor=Color.White;
        adjustments.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,43)); adjustments.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,14)); adjustments.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,43));
        ConfigureBar(sizeBar,24,96,40,0,0,200); sizeBar.LargeChange=8;
        ConfigureBar(speedBar,1,20,Native.Speed,0,0,200);
        adjustments.Controls.Add(SliderSection("02   指针大小",sizeBar,sizeValue,"小","大","选择与调整后自动保存"),0,0);
        preview.Dock=DockStyle.Fill; preview.Margin=new Padding(4,26,4,26); adjustments.Controls.Add(preview,1,0);
        adjustments.Controls.Add(SliderSection("03   鼠标灵敏度",speedBar,speedValue,"慢","快","Windows 原生 20 档 · 非硬件 DPI"),2,0);
        layout.Controls.Add(adjustments,0,3);
        tips.SetToolTip(sizeBar,"指针大小：24–96 px"); tips.SetToolTip(speedBar,"Windows 原生速度：1–20 档");

        var startup=Table(1,2); startup.Padding=new Padding(0,12,0,6);
        startup.RowStyles.Add(new RowStyle(SizeType.Absolute,29)); startup.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        startupCheck.Text="开机自启，自动应用上次配置"; startupCheck.AutoSize=true; startupCheck.Dock=DockStyle.Fill; startupCheck.Margin=Padding.Empty; startupCheck.ForeColor=Ink;
        startupHint.Text="登录 Windows 后在托盘运行；样式、配色、大小和速度自动记忆。"; startupHint.Dock=DockStyle.Fill; startupHint.Font=new Font("Microsoft YaHei UI",12,FontStyle.Regular,GraphicsUnit.Pixel); startupHint.ForeColor=Muted; startupHint.Margin=Padding.Empty;
        startup.Controls.Add(startupCheck,0,0); startup.Controls.Add(startupHint,0,1); layout.Controls.Add(startup,0,4);
        startupCheck.CheckedChanged+=delegate { OnStartupChanged(); };

        var footer=Table(2,1); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,176));
        var footerText=Table(1,2); footerText.RowStyles.Add(new RowStyle(SizeType.Percent,50)); footerText.RowStyles.Add(new RowStyle(SizeType.Percent,50));
        status.Dock=DockStyle.Fill; status.ForeColor=Green; status.TextAlign=ContentAlignment.MiddleLeft; status.AutoEllipsis=true; status.Text="准备就绪 · 点击卡片应用，右侧色块选择颜色"; status.Margin=Padding.Empty;
        footerText.Controls.Add(status,0,0); footerText.Controls.Add(LabelText("关闭窗口后驻留托盘；Ctrl+Q 退出并恢复。",12,FontStyle.Regular),0,1);
        footer.Controls.Add(footerText,0,0);
        var restore=new Button {Text="恢复原设置",Dock=DockStyle.Fill,Margin=new Padding(0,12,0,12),FlatStyle=FlatStyle.Flat,BackColor=Green,ForeColor=Color.White}; restore.FlatAppearance.BorderSize=0; restore.Click+=delegate { Restore(); }; footer.Controls.Add(restore,1,0);
        layout.Controls.Add(footer,0,5);
        viewport.SizeChanged+=delegate { FitLayout(); }; FitLayout();
    }
    TableLayoutPanel SliderSection(string title,TrackBar bar,Label value,string low,string high,string help) {
        var group=Table(1,4); group.Padding=new Padding(18,16,18,10);
        group.RowStyles.Add(new RowStyle(SizeType.Absolute,40)); group.RowStyles.Add(new RowStyle(SizeType.Percent,100)); group.RowStyles.Add(new RowStyle(SizeType.Absolute,24)); group.RowStyles.Add(new RowStyle(SizeType.Absolute,30));
        var heading=Table(2,1); heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,72));
        heading.Controls.Add(LabelText(title,18,FontStyle.Bold),0,0); value.Dock=DockStyle.Fill; value.Margin=Padding.Empty; value.TextAlign=ContentAlignment.MiddleRight; heading.Controls.Add(value,1,0); group.Controls.Add(heading,0,0);
        bar.Dock=DockStyle.Fill; bar.AutoSize=false; bar.Margin=new Padding(0,14,0,4); group.Controls.Add(bar,0,1);
        var ends=Table(2,1); ends.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); ends.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        ends.Controls.Add(LabelText(low,12,FontStyle.Regular),0,0); var right=LabelText(high,12,FontStyle.Regular); right.TextAlign=ContentAlignment.MiddleRight; ends.Controls.Add(right,1,0); group.Controls.Add(ends,0,2);
        group.Controls.Add(LabelText(help,12,FontStyle.Regular),0,3); return group;
    }
    void FitLayout() {
        if(sizingLayout || layout.Parent==null) return;
        sizingLayout=true;
        try {
            float scale=AutoScaleFactor.Height; if(scale<=0) scale=1;
            // Absolute rows have already been DPI-scaled by WinForms.
            float fixedHeight=0; foreach(RowStyle row in layout.RowStyles) if(row.SizeType==SizeType.Absolute) fixedHeight+=row.Height;
            int minimum=(int)Math.Ceiling(fixedHeight+306*scale);
            layout.Height=Math.Max(minimum,viewport.ClientSize.Height-viewport.Padding.Vertical);
        } finally { sizingLayout=false; }
    }
}
