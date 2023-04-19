using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Markup;
using System.Windows.Input;
using System.Windows.Media;
using _3DTools;
using System.Windows.Threading;
namespace WpfCube
{
    public delegate void MouseMoveEventHandler(object sender, MouseEventArgs args, Vector3D vec,long timer);
    public delegate void AnimateCompleteEventHandler(object sender);
    public class Cube : UserControl
    {
        //回転処理時のイベント、ハンドラーは生成側で準備する
        public new event MouseMoveEventHandler OnMouseMove;
        public event AnimateCompleteEventHandler OnAnimateComplete;
        ModelVisual3D _modelVisual3D = new ModelVisual3D();
        ContainerUIElement3D _containerUIElement3D = new ContainerUIElement3D();
        Model3DGroup _m3dg = new Model3DGroup();
        TranslateTransform3D _translate;
        RotateTransform3D _rotate = new RotateTransform3D();
        Transform3DGroup _transformGroup = new Transform3DGroup();
        ModelUIElement3D _modelUIElement = new ModelUIElement3D();
        Point _startPos;
        bool _bMouseDown = false;
        public QuaternionAnimation _qtAnimation = new QuaternionAnimation();
        QuaternionRotation3D _quaternionRotation3D = new QuaternionRotation3D(); 
        Quaternion? _qt = new Quaternion();
        //public Quaternion _qtTrack = new Quaternion();
        Viewport3D _viewPort;
        MainWindow _wndMain;
        Vector3D _afterRotation = new Vector3D();
        public Vector3D _hitFrontNormal = new Vector3D(0,0,1);
        public Vector3D _hitTopNormal = new Vector3D(0, 1, 0);
        public Vector3D _hitRightNormal = new Vector3D(1, 0, 0);
        Vector3D _hitFaceNormal = new Vector3D();
        Trackball _track;
        ScreenSpaceLines3D _normalLineX;// = new ScreenSpaceLines3D();
        ScreenSpaceLines3D _normalLineY;// = new ScreenSpaceLines3D();
        ScreenSpaceLines3D _normalLineZ;// = new ScreenSpaceLines3D();
        public Vector3D _normalX = new Vector3D(1, 0, 0);
        public Vector3D _normalY = new Vector3D(0, 1, 0);
        public Vector3D _normalZ = new Vector3D(0, 0, 1);
        Vector3D _axisX = new Vector3D(1, 0, 0);
        Vector3D _axisY = new Vector3D(0, 1, 0);
        Vector3D _axisZ = new Vector3D(0, 0, 1);
        DispatcherTimer _dispatcherTimer;
        public int _cubeID = 0;
        long _timeCount = 0;
        public CUBEKIND _kind = CUBEKIND.NONE;
        ToolTip _toolTip = new ToolTip();

        //現在（回転後）の座標を返す
        public Vector3D Translate
        {
            get
            {
                return _afterRotation;
            }
        }
        public double Angle
        {
            get
            {
                Quaternion q = (Quaternion)_qt;
                return q.Angle;
            }
        }
        //回転と位置を初期状態にする
        public void ReSet()
        {
            //_qtAnimation.FillBehavior = FillBehavior.Stop;
            _qt = Quaternion.Identity;
            Rotate(new Vector3D(0, 0, 1), 0);
            //_qtAnimation.FillBehavior = FillBehavior.HoldEnd;
            //_s.Seek(TimeSpan.Zero);
            _s.Remove(this);
            //_s.SkipToFill(this);
        }
        public void Clear()
        {
            //_normalX.E
            if (_viewPort.Children.Contains(_modelVisual3D))
            {
                _viewPort.Children.Remove(_modelVisual3D);
            }
        }
        public double AnimTime
        {
            set
            {
                _qtAnimation.Duration = new Duration(TimeSpan.FromMilliseconds((double)value));
            }
            
        }
        //CTor
        public Cube(MainWindow wndMain, Viewport3D viewPort, int id, double offX, double offY, double offZ, double animtime, Trackball track, CUBEKIND kind, bool showaxis = false)
        {
            _kind = kind;
            _modelVisual3D.Children.Clear();
            _track = track;
            _wndMain = wndMain;
            _viewPort = viewPort;
            _cubeID = id;
            if (id == -1 && kind == CUBEKIND.NONE)
            {
                return;
            }
            //QuaternionRotation3Dをリソースに保存する
            NameScope.SetNameScope(this, new NameScope());
            INameScope nameScope = NameScope.GetNameScope(this);
            nameScope.RegisterName("QuaternionRotation3D", _quaternionRotation3D);
            //平行移動
            _translate = new TranslateTransform3D(offX, offY, offZ);
            _transformGroup.Children.Add(_translate);
            //回転移動
            _transformGroup.Children.Add(_rotate);
            _containerUIElement3D.Transform = _transformGroup;
            //リソース(App.xamlで定義)から立方体のモデルを生成

            GeometryModel3D m3d;
            m3d = (GeometryModel3D)Application.Current.Resources["正面"];
            m3d = m3d.Clone();
            if ((kind & CUBEKIND.FRONT) == CUBEKIND.FRONT)
            {
                m3d.Material = (MaterialGroup)Application.Current.Resources["FrontMaterial"];
            }
            else
            {
                m3d.Material = (MaterialGroup)Application.Current.Resources["InnerMaterial"];
            }
            _m3dg.Children.Add(m3d);

            m3d = (GeometryModel3D)Application.Current.Resources["背面"];
            m3d = m3d.Clone();
            if ((kind & CUBEKIND.BACK) == CUBEKIND.BACK)
            {
                m3d.Material = (MaterialGroup)Application.Current.Resources["BackMaterial"];
            }
            else
            {
                m3d.Material = (MaterialGroup)Application.Current.Resources["InnerMaterial"];
            }
            _m3dg.Children.Add(m3d);

            m3d = (GeometryModel3D)Application.Current.Resources["左面"];
            m3d = m3d.Clone();
            if ((kind & CUBEKIND.LEFT) == CUBEKIND.LEFT)
            {
                m3d.Material = (MaterialGroup)Application.Current.Resources["LeftMaterial"];
            }
            else
            {
                m3d.Material = (MaterialGroup)Application.Current.Resources["InnerMaterial"];
            }
            _m3dg.Children.Add(m3d);

            m3d = (GeometryModel3D)Application.Current.Resources["右面"];
            m3d = m3d.Clone();
            if ((kind & CUBEKIND.RIGHT) == CUBEKIND.RIGHT)
            {
                m3d.Material = (MaterialGroup)Application.Current.Resources["RightMaterial"];
            }
            else
            {
                m3d.Material = (MaterialGroup)Application.Current.Resources["InnerMaterial"];
            }
            _m3dg.Children.Add(m3d);

            m3d = (GeometryModel3D)Application.Current.Resources["底面"];
            m3d = m3d.Clone();
            if ((kind & CUBEKIND.BOTTOM) == CUBEKIND.BOTTOM)
            {
                m3d.Material = (MaterialGroup)Application.Current.Resources["BottomMaterial"];
            }
            else
            {
                m3d.Material = (MaterialGroup)Application.Current.Resources["InnerMaterial"];
            }
            _m3dg.Children.Add(m3d);

            m3d = (GeometryModel3D)Application.Current.Resources["上面"];
            m3d = m3d.Clone();
            if ((kind & CUBEKIND.TOP) == CUBEKIND.TOP)
            {
                m3d.Material = (MaterialGroup)Application.Current.Resources["TopMaterial"];
            }
            else
            {
                m3d.Material = (MaterialGroup)Application.Current.Resources["InnerMaterial"];
            }
            _m3dg.Children.Add(m3d);

            _modelUIElement.Model = _m3dg;
            //_modelUIElement.Model = (Model3D)Application.Current.Resources["Cube"];
            _containerUIElement3D.Children.Add(_modelUIElement);
            //マウスのイベントハンドラー設定
            _modelUIElement.MouseDown += new MouseButtonEventHandler(_modelUIElement_MouseDown);
            _modelUIElement.MouseUp += new MouseButtonEventHandler(_modelUIElement_MouseUp);
            _modelUIElement.MouseMove += new MouseEventHandler(_modelUIElement_MouseMove);
            _modelUIElement.MouseLeave += new MouseEventHandler(_modelUIElement_MouseLeave);
            _modelUIElement.MouseEnter += new MouseEventHandler(_modelUIElement_MouseEnter);
            _modelVisual3D.Children.Add(_containerUIElement3D);

            if (offX == 0 && offY == 0 && offZ == 0)
            {
                _normalLineX = new ScreenSpaceLines3D();
                _normalLineY = new ScreenSpaceLines3D();
                _normalLineZ = new ScreenSpaceLines3D();
                int len = 5;
                if (showaxis)
                {
                    _normalLineX.Thickness = 2;
                    _normalLineY.Thickness = 2;
                    _normalLineZ.Thickness = 2;
                }
                else
                {
                    _normalLineX.Thickness = 0;
                    _normalLineY.Thickness = 0;
                    _normalLineZ.Thickness = 0;
                }
                _normalLineX.Points.Add(new Point3D(offX, offY, offZ));
                _normalLineX.Points.Add(new Point3D(offX + len, offY, offZ));
                _normalLineX.Color = Colors.Red;
                _normalLineY.Points.Add(new Point3D(offX, offY, offZ));
                _normalLineY.Points.Add(new Point3D(offX, offY + len, offZ));
                _normalLineY.Color = Colors.Green;
                _normalLineZ.Points.Add(new Point3D(offX, offY, offZ));
                _normalLineZ.Points.Add(new Point3D(offX, offY, offZ + len));
                _normalLineZ.Color = Colors.Blue;
                _normalLineX.Transform = _rotate;
                _normalLineY.Transform = _rotate;
                _normalLineZ.Transform = _rotate;

                _track._axisX = _normalLineX;
                _track._axisY = _normalLineY;
                _track._axisZ = _normalLineZ;
                viewPort.Children.Add(_normalLineX);
                viewPort.Children.Add(_normalLineY);
                viewPort.Children.Add(_normalLineZ);
            }
            viewPort.Children.Add(_modelVisual3D);
            
            //アニメーション設定
            _qtAnimation.Completed += new EventHandler(_qtAnimation_Completed);
            _qtAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(animtime));
            _qtAnimation.FillBehavior = FillBehavior.HoldEnd;
            Storyboard.SetTargetName(_qtAnimation, "QuaternionRotation3D");
            //Storyboard.SetTarget(_qtAnimation, _quaternionRotation3D);//ではだめぽ

            Storyboard.SetTargetProperty(_qtAnimation, new PropertyPath(QuaternionRotation3D.QuaternionProperty));
            _rotate.Rotation = _quaternionRotation3D;
            _s.Children.Clear();
            //s.Completed += new EventHandler(s_Completed);
            _s.Children.Add(_qtAnimation);
            //タイマー
            _dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _dispatcherTimer.Interval = new TimeSpan(10000);//ナノ秒
            _dispatcherTimer.Tick += new EventHandler(_dispatcherTimer_Tick);
             
            //位置を保存
            SetLastPosition();
            //_toolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            //_toolTip.PlacementTarget = this;
            //ToolTipService.SetToolTip(this, "AAAAA");
        }

        void _modelUIElement_MouseEnter(object sender, MouseEventArgs e)
        {
            Console.WriteLine("Cube[{0}]:({1},{2},{3}) normal:({4},{5},{6})", _cubeID, Translate.X, Translate.Y, Translate.Z, _normalZ.X, _normalZ.Y, _normalZ.Z);
            //string str = "AAAAA";
            //
            //_toolTip.IsOpen = true;
            //this.ToolTip.
        }

        void _dispatcherTimer_Tick(object sender, EventArgs e)
        {
            _timeCount++;
        }
        void _modelUIElement_MouseLeave(object sender, MouseEventArgs e)
        {
            _dispatcherTimer.Stop();
            Mouse.OverrideCursor = null;
        }
        void _modelUIElement_MouseMove(object sender, MouseEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Hand;
            e.Handled = true;
            if (!_bMouseDown) return;
            if (!_dispatcherTimer.IsEnabled)
            {
                _dispatcherTimer.Start();
            }
            //現在のマウス座標
            Point p = e.GetPosition((ModelUIElement3D)sender);
            int dir1 = 1;
            int dir2 = 1;
            Vector3D vec3d = new Vector3D();
            Vector3D vec3d1 = new Vector3D();
            Vector3D vec3d2 = new Vector3D();
            double between1 = 0;
            double between2 = 0;
            double angle = 0;
            //移動量を算出して判定
            double dx = (p.X - _startPos.X);
            double dy = (_startPos.Y - p.Y);
            if (dx == 0 && dy == 0) return;
            double x = 0;
            double y = 0;
            double z = 0;
            double theta = 90;
            Quaternion qt;
            if (Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2)) > 10)
            {
                _dispatcherTimer.Stop();
                if (Math.Abs(_hitFaceNormal.X) == 1)
                {
                    x = dx;
                    y = dy;
                    if (_hitRightNormal.Z < 0)
                    {
                        x *= -1;
                        y *= -1;
                    }
                    z = 0;
                    qt = new Quaternion(new Vector3D(x, y, z), theta);
                    vec3d2.X = x; vec3d2.Y = y; vec3d2.Z = z;
                    vec3d2.Normalize();
                    vec3d1 = Vector3D.CrossProduct(_hitRightNormal, vec3d2);
                    between1 = Vector3D.AngleBetween(_hitTopNormal, vec3d1);
                    between2 = Vector3D.AngleBetween(_hitFrontNormal , vec3d1);
                    if (between1 > 90)
                    {
                        between1 = 180 - between1;
                        dir1 = -1;
                    }
                    if (between2 > 90)
                    {
                        between2 = 180 - between2;
                        dir2 = -1;
                    }
                    if (Math.Abs(between1) < Math.Abs(between2))
                    {
                        vec3d = dir1 * _axisY;
                    }
                    else
                    {
                        vec3d = dir2 * _axisZ;
                    }
                    Console.WriteLine("Vec3D({0},{1},{2})", vec3d.X, vec3d.Y, vec3d.Z);
                }
                if (Math.Abs(_hitFaceNormal.Y) == 1)
                {
                    x = dx;
                    y = dy;
                    if (_hitTopNormal.Z < 0)
                    {
                        x *= -1;
                        y *= -1;
                    }
                    z = 0;
                    qt = new Quaternion(new Vector3D(x, y, z), theta);
                    vec3d2.X = x; vec3d2.Y = y; vec3d2.Z = z;
                    vec3d2.Normalize();
                    vec3d1 = Vector3D.CrossProduct(_hitTopNormal, vec3d2);
                    between1 = Vector3D.AngleBetween(_hitRightNormal, vec3d1);
                    between2 = Vector3D.AngleBetween(_hitFrontNormal, vec3d1);

                    if (between1 > 90)
                    {
                        between1 = 180 - between1;
                        dir1 = -1;
                    }
                    if (between2 > 90)
                    {
                        between2 = 180 - between2;
                        dir2 = -1;
                    }
                    if (Math.Abs(between1) < Math.Abs(between2))
                    {
                        vec3d = dir1 * _axisX;
                    }
                    else
                    {
                        vec3d = dir2 * _axisZ;
                    }
                }
                if (Math.Abs(_hitFaceNormal.Z) == 1)
                {
                    x = dx;
                    y = dy;
                    if (_hitFrontNormal.Z < 0)
                    {
                        x *= -1;
                        y *= -1;
                    }
                    z = 0;
                    qt = new Quaternion(new Vector3D(x, y, z), theta);
                    vec3d2.X = x; vec3d2.Y = y; vec3d2.Z = z;
                    vec3d2.Normalize();
                    vec3d1 = Vector3D.CrossProduct(_hitFrontNormal, vec3d2);
                    between1 = Vector3D.AngleBetween(_hitTopNormal, vec3d1);
                    between2 = Vector3D.AngleBetween(_hitRightNormal, vec3d1);
                    if (between1 > 90)
                    {
                        between1 = 180 - between1;
                        dir1 = -1;
                    }
                    if (between2 > 90)
                    {
                        between2 = 180 - between2;
                        dir2 = -1;
                    }
                    if(Math.Abs(between1) < Math.Abs(between2)){
                        vec3d = dir1 * _axisY;
                    }else{
                        vec3d = dir2 * _axisX;
                    }
                }
                Console.WriteLine("Angle:{0},{1}", between1, between2);
                _bMouseDown = false;
                OnMouseMove(this, e, vec3d,_timeCount);
                Console.WriteLine("vec3d({0},{1},{2}) normal({3},{4},{5}) xyz({6},{7},{8}))", vec3d.X, vec3d.Y, vec3d.Z, _hitFaceNormal.X, _hitFaceNormal.Y, _hitFaceNormal.Z, x, y, z);
            }

        }
        Vector3D SurfaceVector(object sender, MouseEventArgs e)
        {
            Vector3D ret = new Vector3D();
            Point pos = e.GetPosition((ModelUIElement3D)sender);
            HitTestResult rawresult_S = VisualTreeHelper.HitTest(_viewPort,_startPos);
            RayHitTestResult rayResult_S = rawresult_S as RayHitTestResult;
            HitTestResult rawresult_E = VisualTreeHelper.HitTest(_viewPort, pos);
            RayHitTestResult rayResult_E = rawresult_E as RayHitTestResult;
            ret.X = rayResult_E.PointHit.X - rayResult_S.PointHit.X;
            ret.Y = rayResult_E.PointHit.Y - rayResult_S.PointHit.Y;
            ret.Z = rayResult_E.PointHit.Z - rayResult_S.PointHit.Z;
            ret = RotateMouse(ret);
            Console.WriteLine("ret({0},{1},{2})", ret.X, ret.Y, ret.Z);
            return ret;
        }
        Vector3D RotateMouse(Vector3D vec3d)
        {
            Vector3D ret;
            Matrix3D mat = Matrix3D.Identity;
            mat.Rotate((Quaternion)_track._q);
            //_hitFaceNormal = Vector3D.Multiply(v3d, mat);
            ret = Vector3D.Multiply(vec3d, mat);
            return ret;
        }

        void _modelUIElement_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _dispatcherTimer.Stop();
            e.Handled = true;
            _bMouseDown = false;
        }
        public void ShowHideAxis(bool bhide)
        {
            if (_normalLineX == null) return;
            if (bhide)
            {
                _normalLineX.Thickness = 0;
                _normalLineY.Thickness = 0;
                _normalLineZ.Thickness = 0;
            }
            else
            {
                _normalLineX.Thickness = 2;
                _normalLineY.Thickness = 2;
                _normalLineZ.Thickness = 2;
            }
        }
        void _modelUIElement_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            //マウスの開始座標を保存
            _startPos = e.GetPosition((ModelUIElement3D)sender);
            _bMouseDown = true;
            HitTest(sender, e);
            //Model3DGroup cube = (Model3DGroup)Application.Current.Resources["Cube"];
            //Model3DGroup model3dg = (Model3DGroup)cube.Children[0];
            //GeometryModel3D geo = (GeometryModel3D)model3dg.Children[0];
            //MeshGeometry3D mesh = (MeshGeometry3D)geo.Geometry;
            //Vector3D vec3d = mesh.Normals[0];
            //RotResult("正面", vec3d);
            //model3dg = (Model3DGroup)cube.Children[3];
            //geo = (GeometryModel3D)model3dg.Children[0];
            //mesh = (MeshGeometry3D)geo.Geometry;
            //vec3d = mesh.Normals[0];
            //RotResult("右面", vec3d);
            //
            //model3dg = (Model3DGroup)cube.Children[5];
            //geo = (GeometryModel3D)model3dg.Children[0];
            //mesh = (MeshGeometry3D)geo.Geometry;
            //vec3d = mesh.Normals[0];
            //RotResult("上面", vec3d);
            _hitTopNormal = RotResult("上面", new Vector3D(0, 1, 0));
            _hitFrontNormal = RotResult("正面", new Vector3D(0, 0, 1));
            _hitRightNormal = RotResult("右面", new Vector3D(1, 0, 0));
            _timeCount = 0;
            //_dispatcherTimer.Start();
        }
        Vector3D RotResult(string face,Vector3D v3d)
        {
            Vector3D ret;
            Matrix3D mat = Matrix3D.Identity;
            mat.Rotate((Quaternion)_track._q);
            //_hitFaceNormal = Vector3D.Multiply(v3d, mat);
            ret = Vector3D.Multiply(v3d, mat);
            ret.X = (ret.X);
            ret.Y = (ret.Y);
            ret.Z = (ret.Z);
            Console.WriteLine("{3}:({0},{1},{2})", ret.X, ret.Y, ret.Z, face);
            return ret;
        }
        /// <summary>
        /// どの面でマウスがクリックされたかを調べる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void HitTest(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            
            //_hitFaceNormal.X = _hitFaceNormal.Y = _hitFaceNormal.Z = 0; ;
            Point mouseposition = args.GetPosition(_viewPort);
            Point3D testpoint3D = new Point3D(mouseposition.X, mouseposition.Y, 0);
            Vector3D testdirection = new Vector3D(mouseposition.X, mouseposition.Y, 10);
            PointHitTestParameters pointparams = new PointHitTestParameters(mouseposition);
            RayHitTestParameters rayparams = new RayHitTestParameters(testpoint3D, testdirection);

            //test for a result in the Viewport3D
            VisualTreeHelper.HitTest(_viewPort, null, HTResult, pointparams);
            //UpdateTestPointInfo(testpoint3D, testdirection);
        }

        public HitTestResultBehavior HTResult(System.Windows.Media.HitTestResult rawresult)
        {
            //MessageBox.Show(rawresult.ToString());
            RayHitTestResult rayResult = rawresult as RayHitTestResult;

            if (rayResult != null)
            {
                RayMeshGeometry3DHitTestResult rayMeshResult = rayResult as RayMeshGeometry3DHitTestResult;

                if (rayMeshResult != null)
                {
                    GeometryModel3D hitgeo = rayMeshResult.ModelHit as GeometryModel3D;
                    MeshGeometry3D mesh = (MeshGeometry3D)hitgeo.Geometry;
                    Matrix3D m3d = hitgeo.Transform.Value;
                    
                    //if (_hitFaceNormal.X == 0 && _hitFaceNormal.Y == 0 && _hitFaceNormal.Z == 0)
                    {
                        Vector3D v3d = mesh.Normals[0];
                        Matrix3D mat = Matrix3D.Identity;
                        mat.Rotate((Quaternion)_qt);
                        _hitFaceNormal = Vector3D.Multiply(v3d, mat);
                        _hitFaceNormal.X = Math.Round(_hitFaceNormal.X);
                        _hitFaceNormal.Y = Math.Round(_hitFaceNormal.Y);
                        _hitFaceNormal.Z = Math.Round(_hitFaceNormal.Z);
                    }
                    //UpdateResultInfo(rayMeshResult);
                    //UpdateMaterial(hitgeo, (side1GeometryModel3D.Material as MaterialGroup));
                    //UpdateMaterial(hitgeo);
                }
            }

            //return HitTestResultBehavior.Continue;
            return HitTestResultBehavior.Stop;//最初の面だけ
        }


        Storyboard _s = new Storyboard();
        public void Rotate(Vector3D axis, double angle)
        {
            {
                _qtAnimation.From = _qt;//前回までのクォータニオン
                _qtAnimation.To = new Quaternion(axis, angle) * _qtAnimation.From;//Fromまで回転したあと新しい軸での回転（逆なので注意）
                //_qtAnimation.BeginTime = new TimeSpan(0);
                //_qtAnimation.Duration = TimeSpan.FromMilliseconds(500);
            }
            //s.Children.Add(_qtAnimation);
            _s.Begin(this,true);
            //this.BeginStoryboard(s);
            //s.Duration = Duration.fro;
            //s.BeginAnimation(QuaternionRotation3D.QuaternionProperty, _qtAnimation);
            _qt = _qtAnimation.To;
            SetLastPosition();
        }

        void s_Completed(object sender, EventArgs e)
        {
            //変換後のクォータニオンを記憶する
            _qt = _qtAnimation.To;
            SetLastPosition();
            if (OnAnimateComplete != null)
            {
                OnAnimateComplete(this);
            }
        }

        void _qtAnimation_Completed(object sender, EventArgs e)
        {
            //変換後のクォータニオンを記憶する
            _qt = _qtAnimation.To;
            SetLastPosition();
            if (OnAnimateComplete != null)
            {
                OnAnimateComplete(this);
            }
        }
        //変換後の位置座標を計算する
        void SetLastPosition()
        {
            Vector3D v3d = new Vector3D(_translate.OffsetX, _translate.OffsetY, _translate.OffsetZ);
            Matrix3D mat = Matrix3D.Identity;
            mat.Rotate((Quaternion)_qt);
            _afterRotation = Vector3D.Multiply(v3d, mat);
            _afterRotation.X = Math.Round(_afterRotation.X) / 2;
            _afterRotation.Y = Math.Round(_afterRotation.Y) / 2;
            _afterRotation.Z = Math.Round(_afterRotation.Z) / 2;
            //_afterRotation.Normalize();
            Matrix3D mat3d = Matrix3D.Identity;
            mat3d.Rotate((Quaternion)_qt);
            _normalX = _axisX * mat3d;
            _normalX.X = Math.Round(_normalX.X);
            _normalX.Y = Math.Round(_normalX.Y);
            _normalX.Z = Math.Round(_normalX.Z);
            _normalY = _axisY * mat3d;
            _normalY.X = Math.Round(_normalY.X);
            _normalY.Y = Math.Round(_normalY.Y);
            _normalY.Z = Math.Round(_normalY.Z);
            _normalZ = _axisZ * mat3d;
            _normalZ.X = Math.Round(_normalZ.X);
            _normalZ.Y = Math.Round(_normalZ.Y);
            _normalZ.Z = Math.Round(_normalZ.Z);
        }
        public void UpdateQuaternion(Quaternion qt)
        {
            //_qtTrack = qt;
            //Rotate(new Vector3D(0, 1, 0), 0);
        }
    }
}
