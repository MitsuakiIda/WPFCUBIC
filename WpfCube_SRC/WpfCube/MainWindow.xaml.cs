using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell.Interop;
namespace WpfCube
{
    public  enum CUBEKIND
    {
        NONE = 0x00,
        FRONT = 0x01,
        BACK = 0x02,
        LEFT = 0x04,
        RIGHT = 0x08,
        TOP = 0x10,
        BOTTOM = 0x20,
        CENTER = 0x40
    }
    
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        ModelVisual3D _modelVisual3D = new ModelVisual3D();
        ContainerUIElement3D _containerUIElement3D = new ContainerUIElement3D();
        TranslateTransform3D _translate = new TranslateTransform3D();
        RotateTransform3D _rotate = new RotateTransform3D();
        Transform3DGroup _transformGroup = new Transform3DGroup();
        ModelUIElement3D _modelUIElement = new ModelUIElement3D();
        List<Cube> _Cubes = new List<Cube>();
        Stack<KeyValuePair<Cube, Vector3D>> _undoBuffer = new Stack<KeyValuePair<Cube, Vector3D>>();
        Cube _cube;
        Trackball _trackBall = new Trackball();
        bool _bAnimating = false;
        long _timeCount = 1;
        DispatcherTimer _dispatcherTimer;
        List<Image> _imges = new List<Image>();
        List<CroppedBitmap> _bmps = new List<CroppedBitmap>();
        DoubleAnimation _dblAnimation = new DoubleAnimation();
        Storyboard _storybooard = new Storyboard();
        Vector3D _axisX = new Vector3D(1, 0, 0);
        Vector3D _axisY = new Vector3D(0, 1, 0);
        Vector3D _axisZ = new Vector3D(0, 0, 1);
        Cube _specialCube;
        public MainWindow()
        {
            WindowStyle = System.Windows.WindowStyle.None;
            Background = new SolidColorBrush(Colors.Transparent);
            AllowsTransparency = true;
            this.Opacity = 1;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            InitializeComponent();
            _dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _dispatcherTimer.Interval = new TimeSpan(1000000);//ナノ秒
            _dispatcherTimer.Tick += new EventHandler(_dispatcherTimer_Tick);
            //img.Source = bmpimage;

            BitmapFrame frame = BitmapFrame.Create(new Uri("pack://application:,,,/WpfCube;component/Images/7segment.bmp", UriKind.Absolute));

            for (int i = 0; i < 10; i++)
            {
                var bmp = new CroppedBitmap();
                bmp.BeginInit();
                bmp.Source = frame;
                bmp.SourceRect = new Int32Rect(i * 32, 0, 32, 56);
                bmp.EndInit();
                _bmps.Add(bmp);
            }
            string name;
            for (int i = 0; i < 4; i++)
            {
                Image img = new Image();
                img.Width = 16;
                //img.Height = 56;
                //img.Stretch = Stretch.None;
                img.Source = _bmps[0];
                img.Opacity = 0.0;
                name = "ImageOpacity" + i.ToString();
                _dblAnimation = new DoubleAnimation();
                _dblAnimation.Duration = new Duration(TimeSpan.FromSeconds(5));
                _dblAnimation.From = 1.0;
                _dblAnimation.To = 0.0;
                this.RegisterName(name, img);
                //Storyboard.SetTargetName(_dblAnimation, name);
                Storyboard.SetTarget(_dblAnimation, img);
                Storyboard.SetTargetProperty(_dblAnimation, new PropertyPath(Image.OpacityProperty));
                _storybooard.Children.Add(_dblAnimation);
                _imges.Add(img);
                dockPanel2.Children.Add(img);
            }
            _trackBall.Reset();
            _trackBall.Attach(this);
            _trackBall.Slaves.Add(viewPort3D);
            _trackBall.Enabled = true;
        }
        int _100milisec = 0;
        void _dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (_100milisec == 0)
            {
                foreach (var img in _imges)
                {
                    img.Opacity = 1.0;
                }
            }
            _imges[3].Source = _bmps[_100milisec % 10];
            _imges[2].Source = _bmps[_100milisec % 100 / 10];
            _imges[1].Source = _bmps[_100milisec % 1000 / 100];
            _imges[0].Source = _bmps[_100milisec % 10000 / 1000];
            if (++_100milisec > 9999) _100milisec = 0;
            //double w = b.Width;
            //throw new NotImplementedException();
        }
        void Initialize(int nums)
        {
            CUBEKIND kind = CUBEKIND.NONE;
            checkBox1.IsChecked = false;
            ShowHideAxis(!(bool)checkBox1.IsChecked);
            double slit = 2.0;
            int s = -nums / 2;
            int e = nums / 2;
            foreach (Cube cube in _Cubes)
            {
                cube.Clear();
            }
            _Cubes.Clear();
            int id = 0;
            for (var x = s; x <= e; x++)
            {
                for (var y = s; y <= e; y++)
                {
                    for (var z = s; z <= e; z++)
                    {
                        if (x == s)
                        {
                            kind |= CUBEKIND.LEFT;
                        }
                        if (y == s)
                        {
                            kind |= CUBEKIND.BOTTOM;
                        }
                        if (z == s)
                        {
                            kind |= CUBEKIND.BACK;
                        }
                        if (x == e)
                        {
                            kind |= CUBEKIND.RIGHT;
                        }
                        if (y == e)
                        {
                            kind |= CUBEKIND.TOP;
                        }
                        if (z == e)
                        {
                            kind |= CUBEKIND.FRONT;
                        }
                        _cube = new Cube(this, viewPort3D, id++, slit * x, slit * y, slit * z, 500, _trackBall, kind, false);
                        _cube.OnMouseMove += new MouseMoveEventHandler(cube_OnMouseMove);
                        _Cubes.Add(_cube);
                        kind = CUBEKIND.NONE;
                    }
                }
            }
            //string str = emanual.Wpf.Utility.SerializeUtility.Serialize(_Cubes); 
            //button1.IsEnabled = false;
            _undoBuffer.Clear();
            //_trackBall.OnQuaternionChanged += new QuaternionChangeEventHandler(_trackBall_OnQuaternionChanged);
            //_specialCube = new Cube(this, viewPort3D, -1, 0, 0, 0, 500, _trackBall, CUBEKIND.NONE);
        }

        void _trackBall_OnQuaternionChanged(object sender, Quaternion qt)
        {
            foreach (var cube in _Cubes)
            {
                cube.UpdateQuaternion(qt);
            }
        }

        void cube_OnMouseMove(object sender, MouseEventArgs args, Vector3D vec,long timer)
        {
            if (_building) return;

            _timeCount = timer;
            Cube cube = (Cube)sender;
            Vector3D vec3d = vec;
            vec3d.Negate();
            button1.IsEnabled = true;
            _undoBuffer.Push(new KeyValuePair<Cube, Vector3D>(cube, vec3d));
            _randoms.Enqueue(new KeyValuePair<Cube, Vector3D>(cube, vec));
            //_randoms.Push(new KeyValuePair<Cube, Vector3D>(cube, vec));
            Cube last = DoRotate(cube, vec);
            AnimateCompleteEventHandler AnimEventHandler = null;
            AnimEventHandler += (s) =>
            {
                Cube cc = (Cube)s;
                cc.OnAnimateComplete -= AnimEventHandler;
                _bAnimating = false;
                if (IsComplete())
                {
                    _dispatcherTimer.Stop();
                    _storybooard.Begin();
                    _100milisec = 0;
                }
            };
            last.OnAnimateComplete += AnimEventHandler;
            foreach (var c in _list)
            {
                c.Rotate(vec, 90);
            }
        }
        Queue<KeyValuePair<Cube, Vector3D>> _randoms = new Queue<KeyValuePair<Cube, Vector3D>>();
        void  Random(int count)
        {
            //count = 3;
            var listin = Enumerable.Range(0, count);
            List<int> listout = new List<int>();
            List<int> listoutvector = new List<int>();
            //ランダムに並び変え（安全な方法らしい）http://www.codinghorror.com/blog/archives/001008.html
            listout = listin.OrderBy(i => Guid.NewGuid()).ToList<int>();
            listoutvector = listin.OrderBy(i => Guid.NewGuid()).ToList<int>();
            _randoms.Clear();
            Reset();
            _100milisec = 0;
            _dispatcherTimer.Stop();
            _storybooard.Stop();
            _imges[3].Source = _bmps[0];
            _imges[2].Source = _bmps[0];
            _imges[1].Source = _bmps[0];
            _imges[0].Source = _bmps[0];
            Vector3D vec3d = new Vector3D();
            //_timeCount = 1;
            foreach (var i in listout)
            {
                int xyz = listoutvector[i] % 3;
                switch(xyz){
                    case 0:
                        vec3d.X = 1;vec3d.Y = 0;vec3d.Z = 0;
                        break;
                    case 1:
                        vec3d.X = 0;vec3d.Y = 1;vec3d.Z = 0;
                        break;
                    case 2:
                        vec3d.X = 0;vec3d.Y = 0;vec3d.Z = 1;
                        break;
                }
                //DoRotate(_Cubes[i], vec3d);
                int index = i % _Cubes.Count;
                if (_Cubes[index]._kind == CUBEKIND.NONE) 
                {
                    Console.WriteLine("");
                    continue; 
                }
                _randoms.Enqueue(new KeyValuePair<Cube, Vector3D>(_Cubes[index], vec3d));
                //_randoms.Push(new KeyValuePair<Cube, Vector3D>(_Cubes[i], vec3d));
                vec3d.Negate();
                _undoBuffer.Push(new KeyValuePair<Cube, Vector3D>(_Cubes[index], vec3d));
            }
            button1.IsEnabled = true;
            RunningAction(_randoms.ToList(),false,250);
        }
        //Storyboard s = new Storyboard();
        List<Cube> _list = new List<Cube>();
        const double _speedcoef = 30; 
        Cube DoRotate(Cube cube, Vector3D vec)
        {
            //s.Children.Clear();
            _list.Clear();
            bool group;
            Cube lastcube = null;
            int groupcnt = 0;
            //Console.WriteLine("DoRotate =======>ID:{0} axis:{1}",cube._cubeID,vec.ToString());
            if (cube._cubeID == -1)
            {
                foreach (var c in _Cubes)
                {
                    _bAnimating = true;
                    lastcube = c;
                    c.AnimTime = _timeCount * _speedcoef;
                    c.Rotate(vec, 90);
                }
                return lastcube;
            }
            foreach (var c in _Cubes)
            {
                group = false;
                if (vec.X != 0)
                {
                    if (Math.Abs(cube.Translate.X - c.Translate.X) == 0)
                    {
                        groupcnt++;
                        group = true;
                    }
                }
                if (vec.Y != 0)
                {
                    if (Math.Abs(cube.Translate.Y - c.Translate.Y) == 0)
                    {
                        groupcnt++;
                        group = true;
                    }
                }
                if (vec.Z != 0)
                {
                    if (Math.Abs(cube.Translate.Z - c.Translate.Z) == 0)
                    {
                        groupcnt++;
                        group = true;
                    }
                }
                if (group)
                {
                    _bAnimating = true;
                    lastcube = c;
                    _list.Add(c);
                    c.AnimTime = _timeCount * _speedcoef;
                    //c.Rotate(vec, 90);
                }
            }
            //s.Begin();
            //Console.WriteLine("<======= DoRotate count:{0} last:{1} time:{2}.{3}", groupcnt, lastcube._cubeID, DateTime.Now.Second, DateTime.Now.Millisecond);
            return lastcube;
        }
        private bool IsComplete()
        {
            bool bret = true;
            foreach (var cube in _Cubes)
            {
                if (cube.Angle >= 90)
                {
                    Console.WriteLine("Normal CubeNo:{0}({1})", cube._cubeID, cube.Angle);
                    bret = false;
                    break;
                }
            }
            if (bret) Console.WriteLine("Complete");
            return bret;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == ModifierKeys.Shift)
            {
                e.Handled = true;
                _trackBall.Enabled = false;
                DragMove();
            }
            else
            {
                Mouse.OverrideCursor = Cursors.ScrollAll;
            }
        }
        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _trackBall.Enabled = true;
            Mouse.OverrideCursor = Cursors.Arrow;

        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers == ModifierKeys.Shift)
            {
                Reset();
                _trackBall.Reset();
                _timeCount = 1;
            }
        }
        private void Reset()
        {
            foreach (var c in _Cubes)
            {
                c.ReSet();

            }

            //RunningAction(_undoBuffer.ToList(), true, 1);
            //_trackBall.Reset();
            _undoBuffer.Clear();
            //button1.IsEnabled = false;
        }
        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            //Console.WriteLine(cb.SelectedItem);
            ComboBoxItem item = (ComboBoxItem)(cb.SelectedItem);
            int nums = int.Parse(item.Content.ToString());
            if (viewPort3D != null)
            {
                Initialize(nums);
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point3D pos = camera.Position;
            if (e.Delta > 0)
            {
                pos.Z++;
            }
            else
            {
                pos.Z--;
            }
            //camera.Position = pos;
        }
        bool _building = false;
        Queue<KeyValuePair<Cube, Vector3D>> que = new Queue<KeyValuePair<Cube, Vector3D>>();
        private void button1_Click(object sender, RoutedEventArgs e)
        {
#if false
            if (_Cubes.Count != 27) return;
            Vector3D vecX = new Vector3D(1,0,0);
            Vector3D vecY = new Vector3D(0,1,0);
            List<Cube> line = new List<Cube>();
            foreach (var cube in _Cubes)
            {
                if (cube._kind == CUBEKIND.FRONT)
                {
                    Quaternion q = Quaternion.Identity;
                    Vector3D vec = cube.Translate;
                    double dx = Math.Round(vec.X);
                    double adx = Math.Abs(dx);
                    double dy = Math.Round(vec.Y);
                    double ady = Math.Abs(dy);
                    double dz = Math.Round(vec.Z);
                    double adz = Math.Abs(dz);
                    //line = SameLine(cube);
                    que.Clear();
                    //Cube newCube = new Cube(this, viewPort3D, -1, 0, 0, 0, 0, _trackBall, CUBEKIND.NONE);
                    Vector3D axis = _axisX;
                    Double angle = 0;
                    //List<Cube> frontCorners = new List<Cube>() { _Cubes[2], _Cubes[8], _Cubes[20], _Cubes[26] };
                    List<Cube> frontCorners = new List<Cube>() { _Cubes[2]};
                    while (true)
                    //foreach(var c in _Cubes)
                    {
                        if (adz != 0)
                        {
                            //que.Enqueue(new KeyValuePair<Cube, Vector3D>(_specialCube, new Vector3D(-dz, 0, 0)));
                            axis = _axisX * -dz;
                            angle = 90;
                            Console.WriteLine("Pattern1");
                            //q = new Quaternion(new Vector3D(-dz, 0, 0), 90);
                            //c.Rotate(_axisX,90 * -dz);
                            break;
                        }
                        if (adx != 0)
                        {
                            //que.Enqueue(new KeyValuePair<Cube, Vector3D>(_specialCube, new Vector3D(0, 0, dx)));
                            axis = _axisZ * dx;
                            angle = 90;
                            Console.WriteLine("Pattern2");
                            //q = new Quaternion(new Vector3D(0, 0, dx), 90);
                            //c.Rotate(_axisZ,90 * dx);
                            break;
                        }
                        if (dy == -1)
                        {
                            Console.WriteLine("Pattern3");
                            //que.Enqueue(new KeyValuePair<Cube, Vector3D>(_specialCube, new Vector3D(1, 0, 0)));
                            //que.Enqueue(new KeyValuePair<Cube, Vector3D>(_specialCube, new Vector3D(1, 0, 0)));
                            axis = _axisX;
                            angle = 180;
                            //q = new Quaternion(new Vector3D(1, 0, 0), 180);
                            //c.Rotate(_axisX,180);
                        }
                        if (dy == 0)
                        {
                            Console.WriteLine("Pattern4");
                            //que.Enqueue(new KeyValuePair<Cube, Vector3D>(_specialCube, new Vector3D(1, 0, 0)));
                            //q = new Quaternion(new Vector3D(1, 0, 0), 90);
                            //c.Rotate(_axisZ, 90);
                        }
                        break;
                    }
                    foreach (var c in _Cubes)
                    {
                        c.AnimTime = 0;
                        c.Rotate(axis, angle);
                    }
                    Vector3D vec1 = cube.Translate;
                    Vector3D vec2 = _Cubes[2].Translate;
                    for (int i = (int)vec2.Y; i < 1; i++)
                    {
                        //que.Enqueue(new KeyValuePair<Cube, Vector3D>(_Cubes[2], -_axisX));
                        DoRotate(_Cubes[2], -_axisX);
                    }
                    vec2 = _Cubes[2].Translate;
                    for (int i = (int)vec2.X; i < 1; i++)
                    {
                        //que.Enqueue(new KeyValuePair<Cube, Vector3D>(_Cubes[2], -_axisY));
                        DoRotate(_Cubes[2], -_axisY);
                    }
                    vec2 = _Cubes[2].Translate;
                    if (vec2.Z < 0)
                    {
                        //que.Enqueue(new KeyValuePair<Cube, Vector3D>(_Cubes[2], -_axisY));
                        DoRotate(_Cubes[2], -_axisY);
                    }
                    foreach (var c in frontCorners)
                    {
                        if (c._normalZ.Z == 1)
                        {
                            que.Enqueue(new KeyValuePair<Cube, Vector3D>(c, _axisX));
                            //DoRotate(c, _axisX);
                            que.Enqueue(new KeyValuePair<Cube, Vector3D>(c, _axisY));
                            //DoRotate(c, _axisY);
                            que.Enqueue(new KeyValuePair<Cube, Vector3D>(c, -_axisX));
                            //DoRotate(c, -_axisX);
                            patternA(c);
                        }
                        if (c._normalZ.X == 1)
                        {
                            que.Enqueue(new KeyValuePair<Cube, Vector3D>(c, -_axisZ));
                            //DoRotate(c, -_axisZ);
                            que.Enqueue(new KeyValuePair<Cube, Vector3D>(c, -_axisY));
                            //DoRotate(c, -_axisY);
                            que.Enqueue(new KeyValuePair<Cube, Vector3D>(c, _axisZ));
                            //DoRotate(c, _axisZ);
                            patternB(c);
                        }
                    }
                    //if(vec2 == new Vector3D(-1,1,1))
                    //{
                    //    if (_Cubes[2]._normalZ.Y != 1)
                    //    {
                    //        Console.WriteLine("Move second cube");
                    //        //_Cubes[2].Rotate(_axisZ, 90);
                    //        //_Cubes[2].Rotate(_axisX, -90);
                    //        que.Enqueue(new KeyValuePair<Cube, Vector3D>(cube, _axisZ));
                    //        que.Enqueue(new KeyValuePair<Cube, Vector3D>(cube, -_axisX));
                    //    }
                    //}
                    //RotateTrackBall(q);
                }
            }
            RunningAction(que.ToList(), true);
#else
            RunningAction(_undoBuffer.ToList(),true,250);
#endif
        }
        void patternA(Cube cube)
        {
            que.Enqueue(new KeyValuePair<Cube, Vector3D>(cube, -_axisX));
            //DoRotate(cube, -_axisX);
            que.Enqueue(new KeyValuePair<Cube, Vector3D>(cube, _axisZ));
            //DoRotate(cube, _axisZ);
            Cube c = GetCubeByPosition(new Vector3D(1,-1,1));
            que.Enqueue(new KeyValuePair<Cube, Vector3D>(c, _axisX));
            //DoRotate(c, _axisX);
            que.Enqueue(new KeyValuePair<Cube, Vector3D>(cube, -_axisZ));
            //DoRotate(cube,  -_axisZ);
            Console.WriteLine("Pattern A");
        }
        void patternB(Cube cube)
        {
            que.Enqueue(new KeyValuePair<Cube, Vector3D>(cube, _axisZ));
            //DoRotate(cube, _axisZ);
            que.Enqueue(new KeyValuePair<Cube, Vector3D>(cube, -_axisX));
            //DoRotate(cube, -_axisX);
            Cube c = GetCubeByPosition(new Vector3D(1, -1, 1));
            que.Enqueue(new KeyValuePair<Cube, Vector3D>(c, -_axisZ));
            //DoRotate(c, -_axisZ);
            que.Enqueue(new KeyValuePair<Cube, Vector3D>(cube, _axisX));
            //DoRotate(cube, _axisX);
            Console.WriteLine("Pattern B");
        }
        Cube GetCubeByPosition(Vector3D vec3d)
        {
            foreach (var cube in _Cubes)
            {
                if (cube.Translate == vec3d)
                {
                    return cube;
                }
            }
            return null;
        }
        List<Cube> SameLine(Cube cube)
        {
            var line = new List<Cube>();
            Vector3D vec = cube.Translate;
            Console.WriteLine("TARGET ({0},{1},{2})", vec.X, vec.Y, vec.Z);
            vec.Normalize();
            double dx = Math.Round(vec.X);
            double adx = Math.Abs(dx);
            double dy = Math.Round(vec.Y);
            double ady = Math.Abs(dy);
            double dz = Math.Round(vec.Z);
            double adz = Math.Abs(dz);
            foreach (var c in _Cubes)
            {
                Console.WriteLine("cube:{0}({1},{2},{3})", c._cubeID, c.Translate.X, c.Translate.Y, c.Translate.Z);
                if (dx != 0)
                {
                    if (Math.Abs(cube.Translate.X - c.Translate.X) < 0.5 && Math.Abs(cube.Translate.Y - c.Translate.Y) < 0.5)
                    {
                        line.Add(c);
                    }
                    continue;
                }
                if (dy != 1)
                {
                    if (Math.Abs(cube.Translate.Y - c.Translate.Y) < 0.5 && Math.Abs(cube.Translate.Z - c.Translate.Z) < 0.5)
                    {
                        line.Add(c);
                    }
                    continue;
                }
                if (dz != 0)
                {
                    if (Math.Abs(cube.Translate.Z - c.Translate.Z) < 0.5 && Math.Abs(cube.Translate.Y - c.Translate.Y) < 0.5)
                    {
                        line.Add(c);
                    }
                    continue;
                }
            }
            return line;
        }
        private void RunningAction(List<KeyValuePair<Cube, Vector3D>> list, bool IsClearUndo = false,double animtime = 1000)
        {
            if (list.Count == 0) return;
            _building = true;
            button1.IsEnabled = false;
            button2.IsEnabled = false;
            foreach (var cube in _Cubes)
            {
                cube.AnimTime = animtime;
            }
            //DoRotate(pair.Key, pair.Value);
            _actions = AnimationSequence(list, IsClearUndo).GetEnumerator();
            RunNextAction();
        }
        private IEnumerator<Action<Action>> _actions;

        private void RunNextAction()
        {
            if (_actions.MoveNext())
                _actions.Current(RunNextAction);
        }

        IEnumerable<Action<Action>> AnimationSequence(List<KeyValuePair<Cube,Vector3D>> list,bool IsClearUndo)
        {
            foreach (var pair in list)
            {
                yield return Animate(pair.Key, pair.Value);
            }
            if (IsClearUndo)
            {
                _undoBuffer.Clear();
                //button1.IsEnabled = false;
            }
            _building = false;
            foreach (var cube in _Cubes)
            {
                cube.AnimTime = 500;
            }
            if (!IsClearUndo)
            {
                _dispatcherTimer.Start();
            }
            else
            {
                if (IsComplete())
                {
                    _dispatcherTimer.Stop();
                    _storybooard.Begin();
                    _100milisec = 0;
                }
            }
            button1.IsEnabled = true;
            button2.IsEnabled = true;

        }
        Cube _lastcube;
        private Action<Action> Animate(Cube cube,Vector3D vec)
        {
            _lastcube = DoRotate(cube, vec);
            DateTime dt = DateTime.Now;
            return completed =>
            {
                {
                    AnimateCompleteEventHandler AnimEventHandler = null;
                    AnimEventHandler += (s) =>
                    {
                        Cube cc = (Cube)s;
                        cc.OnAnimateComplete -= AnimEventHandler;
                        //Console.WriteLine("OnAnimateComplete:{0} {1}.{2} {3}", cc._cubeID, DateTime.Now.Second, DateTime.Now.Millisecond,DateTime.Now - dt);
                        completed();
                    };
                    _lastcube.OnAnimateComplete += AnimEventHandler;
                    foreach (var c in _list)
                    {
                        c.Rotate(vec, 90);
                    }
                }
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBoxItem item = (ComboBoxItem)comboBox1.SelectedItem;
            int nums = int.Parse(item.Content.ToString());
            Initialize(nums);

        }


        private void checkBox1_Click(object sender, RoutedEventArgs e)
        {
            ShowHideAxis(!(bool)checkBox1.IsChecked);
        }
        private void ShowHideAxis(bool ishide)
        {
            foreach (var cube in _Cubes)
            {
                cube.ShowHideAxis(ishide);
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            //Reset();
            Random(_Cubes.Count);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            double angle = 90;
            Vector3D ret;
            Matrix3D mat = Matrix3D.Identity;
            Quaternion q = Quaternion.Identity;
            if (e.Key == Key.Left)
            {
                q = new Quaternion(new Vector3D(0, 1, 0), -angle);
            }
            if (e.Key == Key.Right)
            {
                q = new Quaternion(new Vector3D(0, 1, 0), angle);
            }
            if (e.Key == Key.Down)
            {
                q = new Quaternion(new Vector3D(1, 0, 0), angle);
            }
            if (e.Key == Key.Up)
            {
                q = new Quaternion(new Vector3D(1, 0, 0), -angle);
            }
            e.Handled = true;
            RotateTrackBall(q);
        }
        void RotateTrackBall(Quaternion q)
        {
            q = _trackBall._q * q;
            _trackBall.Update(q);
        }

    }
    static class Test
    {
        public static void TestMethid(this MainWindow m)
        {
            Console.WriteLine("TestMethod");
        }
    }
}
