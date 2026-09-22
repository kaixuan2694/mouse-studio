using System;
using System.Drawing;
using System.Drawing.Drawing2D;

static partial class Art {
    // Every ornament joins a visible tip at (5,4), matching the system hotspot.
    static void Oval(Graphics g,Brush fill,Pen edge,float x,float y,float w,float h) { g.FillEllipse(fill,x,y,w,h); g.DrawEllipse(edge,x,y,w,h); }
    static void Face(Graphics g,Pen edge,float x,float y) {
        using(var ink=new SolidBrush(edge.Color)) { g.FillEllipse(ink,x,y,3,4); g.FillEllipse(ink,x+13,y,3,4); }
        g.DrawArc(edge,x+5,y+4,6,5,0,180);
    }
    static void DrawCute(Graphics g,int s,Brush fill,Pen pen) {
        using(var light=new SolidBrush(Detail(s,Color.FromArgb(255,242,221)))) {
            Poly(g,fill,pen,5,4,31,18,18,33);
            switch(s) {
                case 20: // Rabbit: long ears and round cheeks.
                    Oval(g,fill,pen,18,9,11,29); Oval(g,fill,pen,35,7,11,31);
                    Oval(g,fill,pen,14,26,39,30); Face(g,pen,25,36); break;
                case 21: // Bear.
                    Oval(g,fill,pen,13,16,16,16); Oval(g,fill,pen,39,16,16,16);
                    Oval(g,fill,pen,15,22,38,34); Oval(g,light,pen,26,36,17,14); Face(g,pen,26,31); break;
                case 22: // Sleeping cloud.
                    using(var path=new GraphicsPath()) { path.AddBezier(17,29,10,14,33,10,37,24); path.AddBezier(37,24,50,14,60,33,49,38); path.AddBezier(49,38,64,55,29,62,19,49); path.AddBezier(19,49,3,49,5,31,17,29); g.FillPath(fill,path); g.DrawPath(pen,path); }
                    g.DrawArc(pen,22,34,7,5,0,180); g.DrawArc(pen,37,34,7,5,0,180); break;
                case 23: // Pudding with caramel cap.
                    Poly(g,fill,pen,22,21,44,21,54,49,48,54,17,54,12,49);
                    Poly(g,light,pen,22,21,44,21,47,30,40,33,33,29,25,33,19,30); Face(g,pen,25,38); break;
                case 24: // Strawberry.
                    using(var path=new GraphicsPath()) { path.AddBezier(14,28,8,12,32,14,33,23); path.AddBezier(33,23,49,8,62,26,50,41); path.AddBezier(50,41,32,67,24,56,14,28); g.FillPath(fill,path); g.DrawPath(pen,path); }
                    Poly(g,light,pen,20,15,31,20,38,12,38,22,48,24,35,28,23,23);
                    g.DrawLine(pen,23,34,24,37); g.DrawLine(pen,39,34,38,37); g.DrawLine(pen,31,45,32,48); break;
                case 25: // Ice cream.
                    Poly(g,light,pen,18,33,49,33,34,59); g.DrawLine(pen,25,39,38,50); g.DrawLine(pen,42,39,30,50);
                    Oval(g,fill,pen,14,13,38,29); Face(g,pen,25,25); break;
                case 26: // Wrapped sweet.
                    Poly(g,fill,pen,13,24,9,43,23,38); Poly(g,fill,pen,44,31,58,25,55,47);
                    Oval(g,fill,pen,19,22,29,25); g.DrawArc(pen,25,25,15,18,-70,140); break;
                case 27: // Chick.
                    Oval(g,fill,pen,15,18,39,36); Poly(g,light,pen,36,32,49,37,36,41);
                    using(var ink=new SolidBrush(pen.Color)) g.FillEllipse(ink,29,29,4,4);
                    g.DrawArc(pen,19,32,15,13,0,160); g.DrawLines(pen,Points(25,53,23,59,18,58)); g.DrawLines(pen,Points(39,53,39,59,45,58)); break;
                case 28: // Penguin.
                    Oval(g,fill,pen,17,14,35,43); Oval(g,light,pen,24,30,23,24); Face(g,pen,26,25);
                    Poly(g,fill,pen,18,31,10,45,18,43); Poly(g,fill,pen,50,31,58,45,50,43); break;
                case 29: // Frog.
                    Oval(g,fill,pen,14,15,18,19); Oval(g,fill,pen,36,15,18,19); Oval(g,fill,pen,12,26,44,28);
                    using(var ink=new SolidBrush(pen.Color)) { g.FillEllipse(ink,21,21,4,5); g.FillEllipse(ink,43,21,4,5); }
                    g.DrawArc(pen,25,35,18,10,0,180); break;
                case 30: // Jellyfish.
                    g.DrawBezier(pen,21,38,9,52,33,50,21,60); g.DrawBezier(pen,34,38,20,52,44,51,34,60); g.DrawBezier(pen,46,38,34,50,57,51,47,58);
                    using(var path=new GraphicsPath()) { path.AddArc(13,14,43,43,180,180); path.AddLines(Points(56,36,48,42,40,37,32,42,23,37,13,36)); g.FillPath(fill,path); g.DrawPath(pen,path); } Face(g,pen,26,27); break;
                case 31: // Snail.
                    using(var path=new GraphicsPath()) { path.AddLines(Points(13,28,19,30,24,45,53,45)); path.AddBezier(53,45,58,60,23,58,17,48); path.CloseFigure(); g.FillPath(fill,path); g.DrawPath(pen,path); }
                    Oval(g,fill,pen,25,18,29,30); g.DrawArc(pen,31,25,16,17,0,290); g.DrawLine(pen,15,31,12,21); g.DrawLine(pen,21,33,24,23); break;
                case 32: // Mushroom.
                    Poly(g,light,pen,26,31,43,31,47,55,23,55); Face(g,pen,27,41);
                    using(var path=new GraphicsPath()) { path.AddArc(10,13,49,40,180,180); path.CloseFigure(); g.FillPath(fill,path); g.DrawPath(pen,path); }
                    g.FillEllipse(light,22,21,8,7); g.FillEllipse(light,39,22,10,8); break;
                case 33: // Paw.
                    Oval(g,fill,pen,12,20,10,14); Oval(g,fill,pen,24,12,11,16); Oval(g,fill,pen,38,15,11,16); Oval(g,fill,pen,49,26,10,14);
                    using(var path=new GraphicsPath()) { path.AddBezier(20,49,14,40,27,29,33,32); path.AddBezier(33,32,43,29,57,48,46,54); path.AddBezier(46,54,38,58,37,50,30,54); path.AddBezier(30,54,23,58,18,55,20,49); g.FillPath(fill,path); g.DrawPath(pen,path); } break;
                case 34: // Gamepad.
                    using(var path=new GraphicsPath()) { path.AddLines(Points(21,23,46,23)); path.AddBezier(46,23,59,26,65,59,49,52); path.AddLines(Points(49,52,41,44,27,44,19,53)); path.AddBezier(19,53,2,60,11,27,21,23); g.FillPath(fill,path); g.DrawPath(pen,path); }
                    g.DrawLine(pen,18,34,30,34); g.DrawLine(pen,24,28,24,40); Oval(g,light,pen,42,29,5,5); Oval(g,light,pen,49,36,5,5); break;
                case 35: // Planet.
                    Oval(g,fill,pen,18,16,34,34);
                    g.DrawBezier(pen,20,24,-1,39,7,62,55,34); g.DrawBezier(pen,55,34,66,21,48,23,49,24); g.FillEllipse(light,28,24,8,8); break;
                case 36: // Little ghost.
                    using(var path=new GraphicsPath()) { path.AddArc(15,15,39,39,180,180); path.AddLines(Points(54,35,54,57,44,50,35,58,25,50,15,57,15,35)); g.FillPath(fill,path); g.DrawPath(pen,path); } Face(g,pen,27,31); break;
                case 37: // Sprout.
                    g.DrawBezier(pen,34,56,38,41,27,35,29,23);
                    using(var path=new GraphicsPath()) { path.AddBezier(31,37,8,38,9,16,13,16); path.AddBezier(13,16,36,16,31,37,31,37); g.FillPath(fill,path); g.DrawPath(pen,path); }
                    using(var path=new GraphicsPath()) { path.AddBezier(33,39,31,18,56,18,56,18); path.AddBezier(56,18,59,41,33,39,33,39); g.FillPath(fill,path); g.DrawPath(pen,path); } break;
                case 38: // Crescent.
                    using(var path=new GraphicsPath()) { path.AddBezier(22,15,61,9,66,60,31,57); path.AddBezier(31,57,7,55,12,23,22,15); path.AddBezier(22,15,13,42,43,52,48,34); path.AddBezier(48,34,49,23,35,12,22,15); g.FillPath(fill,path); g.DrawPath(pen,path); } break;
                case 39: // Kite.
                    Poly(g,fill,pen,5,4,39,13,49,43,17,34); g.DrawLine(pen,5,4,49,43); g.DrawLine(pen,39,13,17,34);
                    g.DrawBezier(pen,49,43,25,44,54,57,39,60); Poly(g,light,pen,39,48,50,46,47,56); break;
                default: throw new ArgumentOutOfRangeException("s");
            }
        }
    }
}
