using StreamKinect2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StreamKinectGUI
{
    public partial class MainForm : Form
    {
        enum State
        {
            STOPPED,
            STARTING,
            STARTED,
        }

        private State m_state;
        private Server m_server;
        private TextBoxTraceListener m_textBoxTraceListener;

        private State CurrentState {
            get { return m_state; }
            set { State old = m_state; m_state = value; ChangedState(old); }
        }

        public MainForm()
        {
            InitializeComponent();

            m_server = new Server();
            m_server.Started += m_server_Started;
            m_server.Stopped += m_server_Stopped;

            m_state = State.STOPPED;

            m_textBoxTraceListener = new TextBoxTraceListener(LogTextBox);
            Trace.Listeners.Add(m_textBoxTraceListener);

            StartServer();
        }

        void m_server_Stopped(Server server)
        {
            CurrentState = State.STOPPED;
        }

        void m_server_Started(Server server)
        {
            CurrentState = State.STARTED;
        }

        private void StartServer()
        {
            Trace.WriteLine("Starting server.");
            m_server.Start();
            CurrentState = State.STARTING;
        }

        private void StopServer()
        {
            Trace.WriteLine("Stopping server.");
            m_server.Stop();
        }

        private void ChangedState(State oldState)
        {
            Debug.WriteLine("Changed state to: " + CurrentState);
            switch(CurrentState)
            {
                case State.STOPPED:
                    StartStopButton.Enabled = true;
                    StartStopButton.Text = "Start";
                    StatusLabel.Text = "Server stopped";
                    break;
                case State.STARTING:
                    StartStopButton.Enabled = false;
                    StartStopButton.Text = "Stop";
                    StatusLabel.Text = "Server starting";
                    break;
                case State.STARTED:
                    StartStopButton.Enabled = true;
                    StartStopButton.Text = "Stop";
                    StatusLabel.Text = "Server running";
                    break;
            }
        }

        private void StartStopButton_Click(object sender, EventArgs e)
        {
            switch (CurrentState)
            {
                case State.STOPPED:
                    StartServer();
                    break;
                case State.STARTED:
                    StopServer();
                    break;
            }

        }
    }
}
