using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics; 
using System.Windows.Forms;
using DiscreteFourierTransform.Mathematics;
using Timer = System.Windows.Forms.Timer;

namespace DiscreteFourierTransform;

public enum DrawingState { User, Fourier }

    
public partial class MainForm : Form
{
      
    private DrawingState currentState = DrawingState.User;
    private List<PointF> drawing = new List<PointF>();      
    private List<Complex> x = new List<Complex>();          
    private List<FourierCoefficient> fourierCoeffs = new List<FourierCoefficient>();
    private List<PointF> path = new List<PointF>();          
    private double time = 0;          

    private Timer timer;

    public MainForm()
    {
        InitializeComponent();
          
          
        timer = new Timer();
        timer.Interval = 16;
        timer.Tick += Timer_Tick;
        timer.Start();
    }

     
    private void MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            currentState = DrawingState.User;
            drawing.Clear();
            x.Clear();
            fourierCoeffs.Clear();
            path.Clear();
            time = 0;
        }
    }

     
    private void MouseMove(object sender, MouseEventArgs e)
    {
        if (currentState == DrawingState.User)
        {
              
            float centerX = this.ClientSize.Width / 2f;
            float centerY = this.ClientSize.Height / 2f;
            drawing.Add(new PointF(e.X - centerX, e.Y - centerY));
            Invalidate(); 
        }
    }

    private void MouseUp(object sender, MouseEventArgs e)
    {
        if (currentState == DrawingState.User)
        {
            currentState = DrawingState.Fourier;
            
            foreach (var pt in drawing)
            {
                x.Add(new Complex(pt.X, pt.Y));
            }
              
            fourierCoeffs = ComputeDFT(x);
              
            fourierCoeffs.Sort((a, b) => b.Amplitude.CompareTo(a.Amplitude));
        }
    }

     
    private List<FourierCoefficient> ComputeDFT(List<Complex> x)
    {
        int N = x.Count;
       
        List<FourierCoefficient> X = new List<FourierCoefficient>();
        
        for (int k = 0; k < N; k++)
        {
            Complex sum = Complex.Zero;
            
            for (int n = 0; n < N; n++)
            {
                double phi = (2 * Math.PI * k * n) / N;
                  
                Complex c = Complex.FromPolarCoordinates(1, -phi);
                sum += x[n] * c;
            }
            sum /= N; 

            FourierCoefficient coef = new FourierCoefficient
            {
                Frequency = k,
                Amplitude = sum.Magnitude,
                Phase = Math.Atan2(sum.Imaginary, sum.Real),
                Coefficient = sum
            };
            X.Add(coef);
        }
        return X;
    }

      
    private void Timer_Tick(object sender, EventArgs e)
    {
        if (currentState == DrawingState.Fourier)
        {
               
            double dt = 2 * Math.PI / fourierCoeffs.Count;
            
            time += dt;
            
            if (time > 2 * Math.PI)
            {
                time = 0;
                // path.Clear();
            }
            
            Invalidate();
        }
    }

     
    private PointF Epicycles(Graphics g, float x, float y, double rotation, List<FourierCoefficient> fourier)
    {
        PointF prevPoint = new PointF(x, y);
        
        foreach (var coef in fourier)
        {
            double freq = coef.Frequency;
            double radius = coef.Amplitude;
            double phase = coef.Phase;
            double angle = freq * time + phase + rotation;
            float newX = (float)(prevPoint.X + radius * Math.Cos(angle));
            float newY = (float)(prevPoint.Y + radius * Math.Sin(angle));
            PointF currentPoint = new PointF(newX, newY);

            
            g.DrawEllipse(Pens.LimeGreen, prevPoint.X - (float)radius, prevPoint.Y - (float)radius, (float)radius * 2, (float)radius * 2);
              
            g.DrawLine(Pens.Red, prevPoint, currentPoint);

            prevPoint = currentPoint;
        }
        return prevPoint;
    }

    
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        
        Graphics g = e.Graphics;
        
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        
        float centerX = this.ClientSize.Width / 2f;
        float centerY = this.ClientSize.Height / 2f;
        
        if (currentState == DrawingState.User)
        {
            if (drawing.Count > 1)
            {
                PointF[] screenPoints = new PointF[drawing.Count];
                
                for (int i = 0; i < drawing.Count; i++)
                {
                    screenPoints[i] = new PointF(drawing[i].X + centerX, drawing[i].Y + centerY);
                }
        
                g.DrawLines(Pens.Black, screenPoints);
                
            }
        }
        else if (currentState == DrawingState.Fourier)
        {
              
            PointF v = Epicycles(g, centerX, centerY, 0, fourierCoeffs);
              
            path.Insert(0, v);
            
            if (path.Count > 1)
            {
                g.DrawLines(new Pen(Color.Blue, 2), path.ToArray());
            }
        }
    }
}

