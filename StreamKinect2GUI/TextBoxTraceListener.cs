using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace StreamKinect2GUI
{
    class TextBoxTraceListener : TraceListener
    {
        private TextBox m_textBox;

        public TextBoxTraceListener(TextBox textBox)
        {
            m_textBox = textBox;
        }

        public override void Write(string message)
        {
            // Ensure what we only update the text box on the UI thread
            // and when the underlying UI element exists.
            if (m_textBox.IsHandleCreated)
            {
                m_textBox.Invoke(new Action(() => m_textBox.AppendText(message)));
            }
        }

        public override void WriteLine(string message)
        {
            Write(message);
            Write("\n");
        }
    }
}
