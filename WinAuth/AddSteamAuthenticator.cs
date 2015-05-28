/*
 * Copyright (C) 2015 Colin Mackie.
 * This software is distributed under the terms of the GNU General Public License.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WinAuth
{
	/// <summary>
	/// Form class for create a new Battle.net authenticator
	/// </summary>
	public partial class AddSteamAuthenticator : ResourceForm
	{
		/// <summary>
		/// Form instantiation
		/// </summary>
		public AddSteamAuthenticator()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Current authenticator
		/// </summary>
		public WinAuthAuthenticator Authenticator { get; set; }

		/// <summary>
		/// Enrolling state
		/// </summary>
		private SteamAuthenticator.EnrollState m_enroll = new SteamAuthenticator.EnrollState();

		/// <summary>
		/// Current enrolling authenticator
		/// </summary>
		private SteamAuthenticator m_steamAuthenticator = new SteamAuthenticator();

		/// <summary>
		/// Set of tab pages taken from the tab control so we can hide and show them
		/// </summary>
		private Dictionary<string, TabPage> m_tabPages = new Dictionary<string, TabPage>();

#region Form Events

		/// <summary>
		/// Load the form
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AddSteamAuthenticator_Load(object sender, EventArgs e)
		{
			nameField.Text = this.Authenticator.Name;

			for (var i=0; i<tabs.TabPages.Count; i++)
			{
				m_tabPages.Add(tabs.TabPages[i].Name, tabs.TabPages[i]);
			}
			tabs.TabPages.RemoveByKey("authTab");
			tabs.TabPages.RemoveByKey("confirmTab");
			tabs.TabPages.RemoveByKey("addedTab");
			tabs.SelectedTab = tabs.TabPages[0];

			revocationcodeField.SecretMode = true;
			revocationcode2Field.SecretMode = true;
			serialField.SecretMode = true;
			secretkeyField.SecretMode = true;

			nameField.Focus();
		}

		/// <summary>
		/// If we close after adding, make sure we save it
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AddSteamAuthenticator_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (m_enroll.Success == true)
			{
				this.DialogResult = System.Windows.Forms.DialogResult.OK;
			}
		}

		/// <summary>
		/// Press the form's cancel button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void cancelButton_Click(object sender, EventArgs e)
		{
			// if we press ESC after adding, make sure we save it
			if (m_enroll.Success == true)
			{
				this.DialogResult = System.Windows.Forms.DialogResult.OK;
			}
		}

		/// <summary>
		/// Click the OK button to verify and add the authenticator
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void confirmButton_Click(object sender, EventArgs e)
		{
			if (activationcodeField.Text.Trim().Length == 0)
			{
				WinAuthForm.ErrorDialog(this, "Please enter the activation code from your email");
				this.DialogResult = System.Windows.Forms.DialogResult.None;
				return;
			}

			m_enroll.ActivationCode = activationcodeField.Text.Trim();

			ProcessEnroll();
		}

		/// <summary>
		/// Select one of the icons
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void iconRift_Click(object sender, EventArgs e)
		{
			steamIconRadioButton.Checked = true;
		}

		/// <summary>
		/// Select one of the icons
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void iconGlyph_Click(object sender, EventArgs e)
		{
			steamAuthenticatorIconRadioButton.Checked = true;
		}

		/// <summary>
		/// Select one of the icons
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void iconArcheAge_Click(object sender, EventArgs e)
		{
			steam2IconRadioButton.Checked = true;
		}

		/// <summary>
		/// Select one of the icons
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void steamIcon_Click(object sender, EventArgs e)
		{
			steam3IconRadioButton.Checked = true;
		}

		/// <summary>
		/// Set the authenticator icon
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void iconRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			if (((RadioButton)sender).Checked == true)
			{
				this.Authenticator.Skin = (string)((RadioButton)sender).Tag;
			}
		}

		/// <summary>
		/// Draw the tabs of the tabcontrol so they aren't white
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
		{
			TabPage page = tabs.TabPages[e.Index];
			e.Graphics.FillRectangle(new SolidBrush(page.BackColor), e.Bounds);

			Rectangle paddedBounds = e.Bounds;
			int yOffset = (e.State == DrawItemState.Selected) ? -2 : 1;
			paddedBounds.Offset(1, yOffset);
			TextRenderer.DrawText(e.Graphics, page.Text, this.Font, paddedBounds, page.ForeColor);

			captchaGroup.BackColor = page.BackColor;
		}

		/// <summary>
		/// Answer the captcha
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void captchaButton_Click(object sender, EventArgs e)
		{
			if (captchacodeField.Text.Trim().Length == 0)
			{
				WinAuthForm.ErrorDialog(this, "Please enter the characters in the image", null, MessageBoxButtons.OK);
				return;
			}

			m_enroll.Username = usernameField.Text.Trim();
			m_enroll.Password = passwordField.Text.Trim();
			m_enroll.CaptchaText = captchacodeField.Text.Trim();

			ProcessEnroll();
		}

		/// <summary>
		/// Login to steam account
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void loginButton_Click(object sender, EventArgs e)
		{
			if (usernameField.Text.Trim().Length == 0 || passwordField.Text.Trim().Length == 0)
			{
				WinAuthForm.ErrorDialog(this, "Please enter your username and password", null, MessageBoxButtons.OK);
				return;
			}

			m_enroll.Username = usernameField.Text.Trim();
			m_enroll.Password = passwordField.Text.Trim();

			ProcessEnroll();
		}

		/// <summary>
		/// Confirm with the code sent by email
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void authcodeButton_Click(object sender, EventArgs e)
		{
			if (authcodeField.Text.Trim().Length == 0)
			{
				WinAuthForm.ErrorDialog(this, "Please enter the authorisation code", null, MessageBoxButtons.OK);
				return;
			}

			m_enroll.EmailAuthText = authcodeField.Text.Trim();

			ProcessEnroll();
		}

		/// <summary>
		/// CLick the close button to save
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void closeButton_Click(object sender, EventArgs e)
		{
			this.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.Close();
		}

		/// <summary>
		/// Handle the enter key on the form
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AddSteamAuthenticator_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == 13)
			{
				switch (tabs.SelectedTab.Name)
				{
					case "loginTab":
						e.Handled = true;
						if (m_enroll.RequiresCaptcha == true)
						{
							captchaButton_Click(sender, new EventArgs());
						}
						else
						{
							loginButton_Click(sender, new EventArgs());
						}
						break;
					case "authTab":
						e.Handled = true;
						authcodeButton_Click(sender, new EventArgs());
						break;
					case "confirmTab":
						e.Handled = true;
						confirmButton_Click(sender, new EventArgs());
						break;
					default:
						e.Handled = false;
						break;
				}

				return;
			}

			e.Handled = false;
		}

		/// <summary>
		/// Enable the button when we have confirmed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void revocationCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			confirmButton.Enabled = revocationCheckbox.Checked;
		}

		/// <summary>
		/// Allow the field to be copied
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void revocationcodeCopy_CheckedChanged(object sender, EventArgs e)
		{
			revocationcodeField.SecretMode = !revocationcodeCopy.Checked;
		}

		/// <summary>
		/// Allow the field to be copied
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void serialCopy_CheckedChanged(object sender, EventArgs e)
		{
			serialField.SecretMode = !serialCopy.Checked;
		}

		/// <summary>
		/// Allow the field to be copied
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void secretCopy_CheckedChanged(object sender, EventArgs e)
		{
			secretkeyField.SecretMode = !secretCopy.Checked;
		}

		/// <summary>
		/// Allow the field to be copied
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void revocationcode2Copy_CheckedChanged(object sender, EventArgs e)
		{
			revocationcode2Field.SecretMode = !revocationcode2Copy.Checked;
		}

#endregion

#region Private methods

		/// <summary>
		/// Process the enrolling calling the authenticator method, checking the state and displaying appropriate tab
		/// </summary>
		private void ProcessEnroll()
		{
			do
			{
				try
				{
					var cursor = Cursor.Current;
					Cursor.Current = Cursors.WaitCursor;
					Application.DoEvents();

					var result = m_steamAuthenticator.Enroll(m_enroll);
					Cursor.Current = cursor;
					if (result == false)
					{
						if (string.IsNullOrEmpty(m_enroll.Error) == false)
						{
							WinAuthForm.ErrorDialog(this, m_enroll.Error, null, MessageBoxButtons.OK);
						}

						if (m_enroll.Requires2FA == true)
						{
							WinAuthForm.ErrorDialog(this, "It looks like you already have an authenticator added to you account", null, MessageBoxButtons.OK);
							return;
						}

						if (m_enroll.RequiresCaptcha == true)
						{
							using (var web = new WebClient())
							{
								byte[] data = web.DownloadData(m_enroll.CaptchaUrl);

								using (var ms = new MemoryStream(data))
								{
									captchaBox.Image = Image.FromStream(ms);
								}
							}
							loginButton.Enabled = false;
							captchaGroup.Visible = true;
							captchacodeField.Text = "";
							captchacodeField.Focus();
							return;
						}
						loginButton.Enabled = true;
						captchaGroup.Visible = false;

						if (m_enroll.RequiresEmailAuth == true)
						{
							if (authoriseTabLabel.Tag == null || string.IsNullOrEmpty((string)authoriseTabLabel.Tag) == true)
							{
								authoriseTabLabel.Tag = authoriseTabLabel.Text;
							}
							string email = string.IsNullOrEmpty(m_enroll.EmailDomain) == false ? "***@" + m_enroll.EmailDomain : string.Empty;
							authoriseTabLabel.Text = string.Format((string)authoriseTabLabel.Tag, email);
							authcodeField.Text = "";
							ShowTab("authTab");
							authcodeField.Focus();
							return;
						}
						if (tabs.TabPages.ContainsKey("authTab") == true)
						{
							tabs.TabPages.RemoveByKey("authTab");
						}

						if (m_enroll.RequiresLogin == true)
						{
							ShowTab("loginTab");
							usernameField.Focus();
							return;
						}

						if (m_enroll.RequiresActivation == true)
						{
							m_enroll.Error = null;

							this.Authenticator.AuthenticatorData = m_steamAuthenticator;
							revocationcodeField.Text = m_enroll.RevocationCode;

							ShowTab("confirmTab");

							activationcodeField.Focus();
							return;
						}

						string error = m_enroll.Error;
						if (string.IsNullOrEmpty(error) == true)
						{
							error = "Unable to add the add the authenticator to your account. Please try again later.";
						}
						WinAuthForm.ErrorDialog(this, error, null, MessageBoxButtons.OK);

						return;
					}

					ShowTab("addedTab");

					revocationcode2Field.Text = m_enroll.RevocationCode;
					serialField.Text = m_steamAuthenticator.Serial;
					secretkeyField.Text = m_enroll.SecretKey;
					tabs.SelectedTab = tabs.TabPages["addedTab"];

					this.closeButton.Location = this.cancelButton.Location;
					this.closeButton.Visible = true;
					this.cancelButton.Visible = false;

					break;
				}
				catch (InvalidEnrollResponseException iere)
				{
					if (WinAuthForm.ErrorDialog(this, "An error occurred while registering the authenticator", iere, MessageBoxButtons.RetryCancel) != System.Windows.Forms.DialogResult.Retry)
					{
						break;
					}
				}
			} while (true);
		}

		/// <summary>
		/// Show the named tab hiding all others
		/// </summary>
		/// <param name="name">name of tab to show</param>
		/// <param name="only">hide all others, or append if false</param>
		private void ShowTab(string name, bool only = true)
		{
			if (only == true)
			{
				tabs.TabPages.Clear();
			}

			if (tabs.TabPages.ContainsKey(name) == false)
			{
				tabs.TabPages.Add(m_tabPages[name]);
			}

			tabs.SelectedTab = tabs.TabPages[name];
		}

#endregion

	}
}
