using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFMVVM_ORACLETEST.Database;
using WPFMVVM_ORACLETEST.Model;

namespace WPFMVVM_ORACLETEST.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        readonly MainWindow ownerWindow = null;
        // MainWindow 객체 선언
        emp _stu = new emp(); 
        public MainViewModel(MainWindow win) 
        { 
            ownerWindow = win; 
        } 
        public string ENAME 
        { 
            get { return _stu.ENAME; } 
            set { 
                _stu.ENAME = value; 
                OnPropertyChanged("ENAME"); 
            } 
        }
        public string JOB 
        { 
            get { return _stu.JOB; } 
            set { 
                _stu.JOB = value; 
                OnPropertyChanged("JOB"); 
                } 
        } 
        ObservableCollection<emp> _sampleDatas = null; 
        public ObservableCollection<emp> SampleDatas 
        { 
            get 
            { 
                if (_sampleDatas == null) 
                { _sampleDatas = new ObservableCollection<emp>(); 
                } 
                return _sampleDatas; 
            } 
            set 
            { _sampleDatas = value; } 
        } //PropertyChaneged 이벤트 선언 및 이벤트 핸들러

        public event PropertyChangedEventHandler PropertyChanged; 
        protected void OnPropertyChanged(string propertyName) 
        { 
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); 
        } 
        /// <summary> 
        /// Connect Command 선언 
        /// </summary> 
        private ICommand connectCommand; 
        public ICommand ConnectCommand 
        { 
            get 
            { return (this.connectCommand) ?? (this.connectCommand = new DelegateCommand(Connect)); 
            } 
        } 
        private ICommand selectCommand; 
        public ICommand SelectCommand 
        { 
            get 
            { 
                return (this.selectCommand) ?? (this.selectCommand = new DelegateCommand(DataSearch)); 
            } 
        } 
        private ICommand _insertCommand; 
        public ICommand InsertCommand 
        { 
            get 
            { return (this._insertCommand) ?? (this._insertCommand = new DelegateCommand(DataInsert)); 
            } 
        } 
        private ICommand loadedCommand; 
        public ICommand LoadedCommand 
        { 
            get 
            { return (this.loadedCommand) ?? (this.loadedCommand = new DelegateCommand(LoadEvent)); 
            } 
        } 
        private void LoadEvent() 
        { //Connect to DB
          if (OracleDBManager.Instance.GetConnection() == false) 
            { 
                string msg = $"Failed to Connect to Database"; 
                MessageBox.Show(msg, "Error"); 
                return; 
            }
            else 
            { 
                string msg = $"Success to Connect to Database"; 
                MessageBox.Show(msg, "Inform"); 
            } 
        } 
        private void DataSearch() 
        { 
            DataSet ds = new DataSet(); 
            string query = @" SELECT EMPNO,ENAME,JOB,MGR,HIREDATE,SAL,COMM,DEPTNO FROM EMP "; 
            OracleDBManager.Instance.ExecuteDsQuery(ds, query); 
            for(int idx = 0; idx < ds.Tables[0].Rows.Count; idx++) 
            {
                emp obj = new emp
                {
                    EMPNO = ds.Tables[0].Rows[idx]["EMPNO"].ToString(),
                    ENAME = ds.Tables[0].Rows[idx]["ENAME"].ToString(),
                    JOB = ds.Tables[0].Rows[idx]["JOB"].ToString(),
                    MGR = ds.Tables[0].Rows[idx]["MGR"].ToString(),
                    HIREDATE = ds.Tables[0].Rows[idx]["HIREDATE"].ToString(),
                    SAL = ds.Tables[0].Rows[idx]["SAL"].ToString(),
                    COMM = ds.Tables[0].Rows[idx]["COMM"].ToString(),
                    DEPTNO = ds.Tables[0].Rows[idx]["DEPTNO"].ToString()
                }; 
                SampleDatas.Add(obj); 
            } 
        } 
        private void DataInsert() 
        { 
            var empList = SampleDatas; 
            foreach (var emp in empList) 
            { 
                string query = @" 
                MERGE INTO STUDENT USING dual ON 
                (EMPNO = '#EMPNO' AND NAME = '#ENAME' AND JOB = '#JOB' AND MGR = '#MGR' AND HIREDATE = '#HIREDATE' AND SAL = '#SAL' AND COMM = '#COMM' AND DEPTNO = '#DEPTNO') 
                WHEN MATCHED THEN UPDATE SET '#EMPNO' AND NAME = '#ENAME' AND JOB = '#JOB' AND MGR = '#MGR' AND HIREDATE = '#HIREDATE' AND SAL = '#SAL' AND COMM = '#COMM' AND DEPTNO = '#DEPTNO' 
                WHEN NOT MATCHED THEN INSERT (EMPNO,ENAME,JOB,MGR,HIREDATE,SAL,COMM,DEPTNO) VALUES ('#EMPNO','#ENAME', '#JOB', '#MGR', '#HIREDATE', '#SAL', '#COMM','#DEPTNO'); ";
                query = query.Replace("#EMPNO", emp.EMPNO);
                query = query.Replace("#NAME", emp.ENAME); 
                query = query.Replace("#JOB", emp.JOB);
                query = query.Replace("#MGR", emp.MGR);
                query = query.Replace("#HIREDATE", emp.HIREDATE);
                query = query.Replace("#SAL", emp.SAL);
                query = query.Replace("#COMM", emp.COMM);
                query = query.Replace("#DEPTNO", emp.DEPTNO);
                OracleDBManager.Instance.ExecuteNonQuery(query); 
            } 
        } /// <summary> /// DB Connect /// </summary> 
        private void Connect() 
        { //Connect to DB
          if (OracleDBManager.Instance.GetConnection() == false) 
          { 
              string msg = $"Failed to Connect to Database"; 
              MessageBox.Show(msg, "Error"); 
              return; 
          } 
          else 
          { 
              string msg = $"Success to Connect to Database"; 
                MessageBox.Show(msg, "Inform"); 
          } 
        } 
    }
}
