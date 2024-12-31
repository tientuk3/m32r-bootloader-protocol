// ecueditor_25.main
// Token: 0x060003D2 RID: 978 RVA: 0x00164A9C File Offset: 0x00162E9C
private void FlashSerial()
{
	byte[] cb = new byte[5];
	int ACK = 6;
	int NAK = 21;
	byte[] buff = new byte[256];
	DateTime endtime = DateTime.Now;
	DateTime starttime = DateTime.Now;
	TimeSpan totaltime = endtime.Subtract(starttime);
	MyProject.Forms.K8FlashStatus.L_elapsedtime.Text = Conversions.ToString(totaltime.Minutes) + ":" + Conversions.ToString(totaltime.Seconds);
	MyProject.Forms.K8FlashStatus.Show();
	MyProject.Forms.K8FlashStatus.Progressbar_Flashstatus.Maximum = 255;
	MyProject.Forms.K8FlashStatus.Progressbar_Flashstatus.Value = 1;
	MyProject.Forms.K8FlashStatus.fmode.ForeColor = Color.DarkGray;
	MyProject.Forms.K8FlashStatus.Progressbar_Flashstatus.Value = 0;
	MyProject.Forms.K8FlashStatus.Progressbar_Flashstatus.Refresh();
	MyProject.Forms.K8FlashStatus.Refresh();
	Application.DoEvents();
	long chksum = CommonFunctions.ReadFlashWord(1048568);
	CommonFunctions.WriteFlashWord(1048568, 0);
	long im = 0L;
	int j;
	long FT_status;
	int x;
	long lngHandle;
	int num3;
	long num5;
	checked
	{
        // calculate checksum
		long chksumflash;
		do
		{
			int i;
			if (i == 0)
			{
				chksumflash += unchecked((long)(checked((int)CommonFunctions.Flash[(int)im] * 256)));
				i = 1;
			}
			else
			{
				i = 0;
				chksumflash += (long)(unchecked((ulong)CommonFunctions.Flash[checked((int)im)]));
			}
			if (chksumflash > 65535L)
			{
				chksumflash -= 65536L;
			}
			im += 1L;
		}
		while (im <= 1048575L);
		chksumflash = (23205L - chksumflash & 65535L);
        // add checksum to end of file
		CommonFunctions.WriteFlashWord(1048568, (int)chksumflash);
		if (MyProject.Forms.K8Datastream.Visible)
		{
			MyProject.Forms.K8Datastream.closeenginedatacomms();
		}
		MyProject.Forms.K8EngineDataViewer.Close();
		MyProject.Forms.K8EngineDataLogger.Close();
		this.B_FlashECU.Enabled = false;
		main.timeBeginPeriod(1);
        // find com port device
		int comportnum = (int)Math.Round(Conversion.Val(Strings.Mid(Conversions.ToString(MySettingsProperty.Settings["ComPort"]), 4)));
		string text = Conversions.ToString(0);
		FT_status = unchecked((long)main.FT_GetNumberOfDevices(ref j, ref text, int.MinValue));
		j--;
		int num = 0;
		int num2 = j;
		int cp;
		for (x = num; x <= num2; x++)
		{
			int iDevice = x;
			num3 = (int)lngHandle;
			long num4 = (long)main.FT_Open(iDevice, ref num3);
			unchecked
			{
				lngHandle = (long)num3;
				FT_status = num4;
				int y;
				FT_status = (long)main.FT_GetComPortNumber(checked((int)lngHandle), ref y);
				if (y == comportnum)
				{
					cp = x;
					x = j;
				}
				FT_status = (long)main.FT_Close(checked((int)lngHandle));
			}
		}
		if (FT_status != 0L)
		{
			Interaction.MsgBox("Could not open com port, please set correct port on K8 enginedata screen. Programming aborted, reset ecu and reprogram", MsgBoxStyle.OkOnly, null);
			this.B_FlashECU.Enabled = true;
			FT_status = unchecked((long)main.FT_Close(checked((int)lngHandle)));
			MyProject.Forms.K8FlashStatus.Close();
			return;
		}
		int iDevice2 = cp;
		num3 = (int)lngHandle;
		num5 = (long)main.FT_Open(iDevice2, ref num3);
	}
	lngHandle = (long)num3;
	FT_status = num5;
    // setup uart config for the device
	FT_status = (long)main.FT_ResetDevice(checked((int)lngHandle), 3);
	checked
	{
		FT_status += unchecked((long)main.FT_Purge(checked((int)lngHandle)));
		FT_status += unchecked((long)main.FT_SetBaudRate(checked((int)lngHandle), 57600));
		FT_status += unchecked((long)main.FT_SetDataCharacteristics(checked((int)lngHandle), 8, 1, 0));
		FT_status += unchecked((long)main.FT_SetTimeouts(checked((int)lngHandle), 50, 50));
		FT_status += unchecked((long)main.FT_SetLatencyTimer(checked((int)lngHandle), 8));
		FT_status += unchecked((long)main.FT_SetUSBParameters(checked((int)lngHandle), 4096, 4096));
	}
	if (FT_status != 0L)
	{
		Interaction.MsgBox("Could not set Com port parameters. Programming aborted, set correct com port for the interface using data monitoring screen", MsgBoxStyle.OkOnly, null);
		this.B_FlashECU.Enabled = true;
		MyProject.Forms.K8FlashStatus.Close();
		FT_status = (long)main.FT_Close(checked((int)lngHandle));
		return;
	}
	FT_status = (long)main.FT_SetDtr(checked((int)lngHandle));
	Thread.Sleep(100);
	int modemstat;
	FT_status = (long)main.FT_GetModemStatus(checked((int)lngHandle), ref modemstat);
	if (FT_status != 0L)
	{
		Interaction.MsgBox("Set the correct Com port for the interface using data monitoring screen", MsgBoxStyle.OkOnly, null);
		MyProject.Forms.K8FlashStatus.Close();
		this.B_FlashECU.Enabled = true;
		MyProject.Forms.K8FlashStatus.Close();
		return;
	}
	if (!(modemstat == 24576 | modemstat == 25088))
	{
		Interaction.MsgBox("Interface is not on or it is not in programming mode, set programming switch to programming mode and retry", MsgBoxStyle.OkOnly, null);
		MyProject.Forms.K8FlashStatus.Close();
		this.B_FlashECU.Enabled = true;
		FT_status = (long)main.FT_ClrDtr(checked((int)lngHandle));
		FT_status = (long)main.FT_Close(checked((int)lngHandle));
		return;
	}
	FT_status = (long)main.FT_SetRts(checked((int)lngHandle));
	Thread.Sleep(300);
	FT_status = (long)main.FT_ClrRts(checked((int)lngHandle));
	Thread.Sleep(300);
    // initialize: send 0x0, wait to receive ACK (0x6), try up to 18 times
	j = 0;
	int rxqueue = 0;
	x = 18;
	int num6 = 1;
	int num7 = x;
	byte txbyte;
	int txqueue;
	int eventstat;
	byte rxbyte;
	checked
	{
		for (j = num6; j <= num7; j++)
		{
			txbyte = 0;
			int lngHandle2 = (int)lngHandle;
			int lngBufferSize = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle2, ref txbyte, lngBufferSize, ref num3);
			Thread.Sleep(40);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			if (rxqueue != 0)
			{
				j = x;
			}
		}
		Thread.Sleep(2);
		int num8 = 1;
		int num9 = rxqueue;
		for (x = num8; x <= num9; x++)
		{
			int lngHandle3 = (int)lngHandle;
			int lngBufferSize2 = 1;
			num3 = 1;
			main.FT_Read_Bytes(lngHandle3, ref rxbyte, lngBufferSize2, ref num3);
		}
	}
	if ((int)rxbyte != ACK)
	{
		Interaction.MsgBox("Unexpected or missing ECU response during intialization. Programming aborted, reset ecu and reprogram." + Conversion.Hex(rxqueue) + " " + Conversion.Hex(rxbyte), MsgBoxStyle.OkOnly, null);
		MyProject.Forms.K8FlashStatus.Close();
		this.B_FlashECU.Enabled = true;
		FT_status = (long)main.FT_ClrDtr(checked((int)lngHandle));
		FT_status = (long)main.FT_Close(checked((int)lngHandle));
		return;
	}
	rxbyte = 0;
	j = 0;
	rxqueue = 0;

	checked
	{
        // write 0x70, wait for ECU to respond something OTHER THAN 0x8C, try up to 10 times
		while (rxqueue == 0 & j < 10)
		{
			txbyte = 112;
			int lngHandle4 = (int)lngHandle;
			int lngBufferSize3 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle4, ref txbyte, lngBufferSize3, ref num3);
			Thread.Sleep(40);
			FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			j++;
		}
		int num10 = 1;
		int num11 = rxqueue;
		for (x = num10; x <= num11; x++)
		{
			int lngHandle5 = (int)lngHandle;
			int lngBufferSize4 = 1;
			num3 = 1;
			main.FT_Read_Bytes(lngHandle5, ref rxbyte, lngBufferSize4, ref num3);
		}
		if (rxbyte != 140) // response not 0x8C -> send unlock code (17 characters)
		{
			txbyte = 245;
			int lngHandle6 = (int)lngHandle;
			int lngBufferSize5 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle6, ref txbyte, lngBufferSize5, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = 132;
			int lngHandle7 = (int)lngHandle;
			int lngBufferSize6 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle7, ref txbyte, lngBufferSize6, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = 0;
			int lngHandle8 = (int)lngHandle;
			int lngBufferSize7 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle8, ref txbyte, lngBufferSize7, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = 0;
			int lngHandle9 = (int)lngHandle;
			int lngBufferSize8 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle9, ref txbyte, lngBufferSize8, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = 12;
			int lngHandle10 = (int)lngHandle;
			int lngBufferSize9 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle10, ref txbyte, lngBufferSize9, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = 83;
			int lngHandle11 = (int)lngHandle;
			int lngBufferSize10 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle11, ref txbyte, lngBufferSize10, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = 85;
			int lngHandle12 = (int)lngHandle;
			int lngBufferSize11 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle12, ref txbyte, lngBufferSize11, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = 69;
			int lngHandle13 = (int)lngHandle;
			int lngBufferSize12 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle13, ref txbyte, lngBufferSize12, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = 70;
			int lngHandle14 = (int)lngHandle;
			int lngBufferSize13 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle14, ref txbyte, lngBufferSize13, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = 73;
			int lngHandle15 = (int)lngHandle;
			int lngBufferSize14 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle15, ref txbyte, lngBufferSize14, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = 77;
			int lngHandle16 = (int)lngHandle;
			int lngBufferSize15 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle16, ref txbyte, lngBufferSize15, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = byte.MaxValue;
			int lngHandle17 = (int)lngHandle;
			int lngBufferSize16 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle17, ref txbyte, lngBufferSize16, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = byte.MaxValue;
			int lngHandle18 = (int)lngHandle;
			int lngBufferSize17 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle18, ref txbyte, lngBufferSize17, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = byte.MaxValue;
			int lngHandle19 = (int)lngHandle;
			int lngBufferSize18 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle19, ref txbyte, lngBufferSize18, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = byte.MaxValue;
			int lngHandle20 = (int)lngHandle;
			int lngBufferSize19 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle20, ref txbyte, lngBufferSize19, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = 86;
			int lngHandle21 = (int)lngHandle;
			int lngBufferSize20 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle21, ref txbyte, lngBufferSize20, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = 48;
			int lngHandle22 = (int)lngHandle;
			int lngBufferSize21 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle22, ref txbyte, lngBufferSize21, ref num3);
			FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			Thread.Sleep(100);
			FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			int num12 = 1;
			int num13 = rxqueue;
            // wait for ACK (0x6)
			for (j = num12; j <= num13; j++)
			{
				int lngHandle23 = (int)lngHandle;
				int lngBufferSize22 = 1;
				num3 = 1;
				main.FT_Read_Bytes(lngHandle23, ref rxbyte, lngBufferSize22, ref num3);
			}

			unchecked
			{
				if ((int)rxbyte != ACK)
				{
					Interaction.MsgBox("No ACK received after sending unlock code. Programming aborted, reset ecu and reprogram", MsgBoxStyle.OkOnly, null);
					MyProject.Forms.K8FlashStatus.Close();
					this.B_FlashECU.Enabled = true;
					FT_status = (long)main.FT_ClrDtr(checked((int)lngHandle));
					FT_status = (long)main.FT_Close(checked((int)lngHandle));
					return;
				}
			}
		}
        // send 0x70 again and wait for some response
		txqueue = 0;
		j = 0;
		FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
		Thread.Sleep(50);
		while (rxqueue == 0 & j < 10)
		{
			txbyte = 112;
			int lngHandle24 = (int)lngHandle;
			int lngBufferSize23 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle24, ref txbyte, lngBufferSize23, ref num3);
			Thread.Sleep(40);
			FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			j++;
		}
	}
	if (j >= 10 | rxqueue == 0)
	{
		Interaction.MsgBox("Error in validating the unlock code from ECU. Programming aborted, reset ecu and reprogram", MsgBoxStyle.OkOnly, null);
		MyProject.Forms.K8FlashStatus.Close();
		this.B_FlashECU.Enabled = true;
		FT_status = (long)main.FT_ClrDtr(checked((int)lngHandle));
		FT_status = (long)main.FT_Close(checked((int)lngHandle));
		return;
	}
    // check that ECU answers 0x8C (this possibly means "access granted")
	checked
	{
		int lngHandle25 = (int)lngHandle;
		int lngBufferSize24 = 1;
		num3 = 1;
		main.FT_Read_Bytes(lngHandle25, ref rxbyte, lngBufferSize24, ref num3);
		Thread.Sleep(50);
		FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
		int lngHandle26 = (int)lngHandle;
		int lngBufferSize25 = 1;
		num3 = 1;
		main.FT_Read_Bytes(lngHandle26, ref rxbyte, lngBufferSize25, ref num3);
		Thread.Sleep(50);
		FT_status += unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
	}
	if (rxbyte != 140 | FT_status != 0L)
	{
		Interaction.MsgBox("Was not able to set the ecu key. Programming aborted, reset ecu and reprogram", MsgBoxStyle.OkOnly, null);
		MyProject.Forms.K8FlashStatus.Close();
		this.B_FlashECU.Enabled = true;
		FT_status = (long)main.FT_ClrDtr(checked((int)lngHandle));
		FT_status = (long)main.FT_Close(checked((int)lngHandle));
		return;
	}
    // write 0x50 and wait for ACK
	txbyte = 80;
	int lngHandle27 = checked((int)lngHandle);
	int lngBufferSize26 = 1;
	num3 = 1;
	main.FT_Write_Bytes(lngHandle27, ref txbyte, lngBufferSize26, ref num3);
	Thread.Sleep(50);
	FT_status = (long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat);
	int num14 = 1;
	int num15 = rxqueue;
	checked
	{
		for (j = num14; j <= num15; j++)
		{
			int lngHandle28 = (int)lngHandle;
			int lngBufferSize27 = 1;
			num3 = 1;
			main.FT_Read_Bytes(lngHandle28, ref rxbyte, lngBufferSize27, ref num3);
		}
	}
	if ((int)rxbyte != ACK)
	{
		Interaction.MsgBox("Status query error 1. Programming aborted, reset ecu and reprogram", MsgBoxStyle.OkOnly, null);
		MyProject.Forms.K8FlashStatus.Close();
		this.B_FlashECU.Enabled = true;
		FT_status = (long)main.FT_ClrDtr(checked((int)lngHandle));
		FT_status = (long)main.FT_Close(checked((int)lngHandle));
		return;
	}
	Thread.Sleep(100);
    // write 0xFF, 0xFF, 0x0F 
	txbyte = byte.MaxValue;
	checked
	{
		int lngHandle29 = (int)lngHandle;
		int lngBufferSize28 = 1;
		num3 = 1;
		main.FT_Write_Bytes(lngHandle29, ref txbyte, lngBufferSize28, ref num3);
		txbyte = byte.MaxValue;
		int lngHandle30 = (int)lngHandle;
		int lngBufferSize29 = 1;
		num3 = 1;
		main.FT_Write_Bytes(lngHandle30, ref txbyte, lngBufferSize29, ref num3);
		txbyte = 15;
		int lngHandle31 = (int)lngHandle;
		int lngBufferSize30 = 1;
		num3 = 1;
		main.FT_Write_Bytes(lngHandle31, ref txbyte, lngBufferSize30, ref num3);
		Thread.Sleep(100);
        // compare ECU ID
		int i = 0;
		int k = 0;
		do
		{
			FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			int num16 = 1;
			int num17 = rxqueue;
			for (j = num16; j <= num17; j++)
			{
				int lngHandle32 = (int)lngHandle;
				int lngBufferSize31 = 1;
				num3 = 1;
				main.FT_Read_Bytes(lngHandle32, ref rxbyte, lngBufferSize31, ref num3);
				if ((i >= 240 & i <= 245) && ((int)rxbyte != CommonFunctions.ReadFlashByte(1048320 + i) & rxbyte != 255) && Interaction.MsgBox("Not same ECU ID in memory and inside the ecu. You can stop the flashing by pressing cancel.", MsgBoxStyle.OkCancel, null) == MsgBoxResult.Cancel)
				{
					MyProject.Forms.K8FlashStatus.Close();
					this.B_FlashECU.Enabled = true;
					unchecked
					{
						FT_status = (long)main.FT_ClrDtr(checked((int)lngHandle));
						FT_status = (long)main.FT_Close(checked((int)lngHandle));
					}
				}
				i++;
			}
			k++;
		}
		while (k <= 255);
		if (Operators.CompareString(CommonFunctions.ECUVersion, "gen2", false) == 0 && Operators.ConditionalCompareObjectEqual(CommonFunctions.ReadFlashLongWord(335632L), 341700, false))
		{
			Thread.Sleep(100);
			txbyte = byte.MaxValue;
			int lngHandle33 = (int)lngHandle;
			int lngBufferSize32 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle33, ref txbyte, lngBufferSize32, ref num3);
			txbyte = 31;
			int lngHandle34 = (int)lngHandle;
			int lngBufferSize33 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle34, ref txbyte, lngBufferSize33, ref num3);
			txbyte = 5;
			int lngHandle35 = (int)lngHandle;
			int lngBufferSize34 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle35, ref txbyte, lngBufferSize34, ref num3);
			Thread.Sleep(100);
			i = 0;
			k = 0;
			do
			{
				FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
				int num18 = 1;
				int num19 = rxqueue;
				for (j = num18; j <= num19; j++)
				{
					int lngHandle36 = (int)lngHandle;
					int lngBufferSize35 = 1;
					num3 = 1;
					main.FT_Read_Bytes(lngHandle36, ref rxbyte, lngBufferSize35, ref num3);
					unchecked
					{
						if (i == 19)
						{
							j = CommonFunctions.ReadFlashByte(335635);
							byte b = rxbyte;
							if (b == 24)
							{
								if (Operators.ConditionalCompareObjectEqual(CommonFunctions.ReadFlashLongWord(335632L), 341700, false))
								{
								}
							}
							else if (b != 196)
							{
								if (b != 255)
								{
									Interaction.MsgBox("Error in reading flashingmode from ECU, programming aborted. Please reboot ecu and reflash", MsgBoxStyle.OkOnly, null);
									CommonFunctions.BlockPgm = true;
									MyProject.Forms.K8FlashStatus.Close();
									this.B_FlashECU.Enabled = true;
									FT_status = (long)main.FT_ClrDtr(checked((int)lngHandle));
									FT_status = (long)main.FT_Close(checked((int)lngHandle));
								}
							}
						}
					}
					i++;
				}
				k++;
			}
			while (k <= 255);
		}
		main.timeBeginPeriod(1);
		Thread.Sleep(300);
		if (CommonFunctions.BlockChanged(0L))
		{
			CommonFunctions.BlockPgm = true;
		}
		if (CommonFunctions.BlockPgm)
		{
			totaltime = DateTime.Now.Subtract(starttime);
			MyProject.Forms.K8FlashStatus.L_elapsedtime.Text = Conversions.ToString(totaltime.Minutes) + ":" + Conversions.ToString(totaltime.Seconds);
			MyProject.Forms.K8FlashStatus.fmode.Text = "Performing full erase, please wait";
			MyProject.Forms.K8FlashStatus.fmode.ForeColor = Color.Gray;
			MyProject.Forms.K8FlashStatus.Progressbar_Flashstatus.Value = 0;
			MyProject.Forms.K8FlashStatus.Progressbar_Flashstatus.Refresh();
			MyProject.Forms.K8FlashStatus.Refresh();
			Application.DoEvents();
			txbyte = 167;
			int lngHandle37 = (int)lngHandle;
			int lngBufferSize36 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle37, ref txbyte, lngBufferSize36, ref num3);
			FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			txbyte = 208;
			int lngHandle38 = (int)lngHandle;
			int lngBufferSize37 = 1;
			num3 = 1;
			main.FT_Write_Bytes(lngHandle38, ref txbyte, lngBufferSize37, ref num3);
			FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
			int loopcount = 0;
			bool loopuntilack = false;
			while (!loopuntilack)
			{
				FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
				while (rxqueue == 0 & j < 100)
				{
					Thread.Sleep(50);
					FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
					j++;
				}
				int num20 = 1;
				int num21 = rxqueue;
				for (j = num20; j <= num21; j++)
				{
					int lngHandle39 = (int)lngHandle;
					int lngBufferSize38 = 1;
					num3 = 1;
					main.FT_Read_Bytes(lngHandle39, ref rxbyte, lngBufferSize38, ref num3);
					if ((int)rxbyte == ACK)
					{
						loopuntilack = true;
					}
				}
				if (loopcount > 10)
				{
					txbyte = 117;
					int lngHandle40 = (int)lngHandle;
					int lngBufferSize39 = 1;
					num3 = 1;
					main.FT_Write_Bytes(lngHandle40, ref txbyte, lngBufferSize39, ref num3);
					FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
					int lngHandle41 = (int)lngHandle;
					int lngBufferSize40 = 1;
					num3 = 1;
					main.FT_Read_Bytes(lngHandle41, ref rxbyte, lngBufferSize40, ref num3);
					txbyte = 80;
					int lngHandle42 = (int)lngHandle;
					int lngBufferSize41 = 1;
					num3 = 1;
					main.FT_Write_Bytes(lngHandle42, ref txbyte, lngBufferSize41, ref num3);
					FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
					int lngHandle43 = (int)lngHandle;
					int lngBufferSize42 = 1;
					num3 = 1;
					main.FT_Read_Bytes(lngHandle43, ref rxbyte, lngBufferSize42, ref num3);
				}
				unchecked
				{
					if (loopcount > 20)
					{
						Interaction.MsgBox("No ACK after full erase, Programming aborted, reset ecu and reprogram.", MsgBoxStyle.OkOnly, null);
						MyProject.Forms.K8FlashStatus.Close();
						this.B_FlashECU.Enabled = true;
						FT_status = (long)main.FT_ClrDtr(checked((int)lngHandle));
						FT_status = (long)main.FT_Close(checked((int)lngHandle));
						CommonFunctions.BlockPgm = true;
						return;
					}
				}
				loopcount++;
			}
		}
		if (Operators.CompareString(CommonFunctions.ECUVersion, "gen2", false) == 0)
		{
			k = Conversions.ToInteger(CommonFunctions.ReadFlashLongWord(335632L));
			if (Operators.ConditionalCompareObjectNotEqual(CommonFunctions.ReadFlashLongWord(335632L), 341700, false))
			{
				MyProject.Forms.K8FlashStatus.fmode.Text = "Normal flash ";
			}
			else
			{
				MyProject.Forms.K8FlashStatus.fmode.Text = "Fast flash ";
			}
		}
		else
		{
			MyProject.Forms.K8FlashStatus.fmode.Text = "Normal flash ";
		}
		MyProject.Forms.K8FlashStatus.fmode.ForeColor = Color.Black;
		MyProject.Forms.K8FlashStatus.Progressbar_Flashstatus.Value = 0;
		MyProject.Forms.K8FlashStatus.Progressbar_Flashstatus.Refresh();
		MyProject.Forms.K8FlashStatus.Refresh();
		Application.DoEvents();
		int startaddr = 0;
		for (int block = startaddr; block <= 15; block++)
		{
			if (CommonFunctions.BlockChanged(unchecked((long)block)) | CommonFunctions.BlockPgm)
			{
				totaltime = DateTime.Now.Subtract(starttime);
				MyProject.Forms.K8FlashStatus.L_elapsedtime.Text = Conversions.ToString(totaltime.Minutes) + ":" + Conversions.ToString(totaltime.Seconds);
				MyProject.Forms.K8FlashStatus.fmode.Text = MyProject.Forms.K8FlashStatus.fmode.Text + Conversion.Hex(block);
				MyProject.Forms.K8FlashStatus.Progressbar_Flashstatus.Refresh();
				MyProject.Forms.K8FlashStatus.Refresh();
				Application.DoEvents();
				if (block == 15)
				{
				}
				txbyte = 32;
				int lngHandle44 = (int)lngHandle;
				int lngBufferSize43 = 1;
				num3 = 1;
				main.FT_Write_Bytes(lngHandle44, ref txbyte, lngBufferSize43, ref num3);
				FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
				txbyte = 0;
				int lngHandle45 = (int)lngHandle;
				int lngBufferSize44 = 1;
				num3 = 1;
				main.FT_Write_Bytes(lngHandle45, ref txbyte, lngBufferSize44, ref num3);
				FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
				txbyte = (byte)block;
				int lngHandle46 = (int)lngHandle;
				int lngBufferSize45 = 1;
				num3 = 1;
				main.FT_Write_Bytes(lngHandle46, ref txbyte, lngBufferSize45, ref num3);
				FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
				txbyte = 208;
				int lngHandle47 = (int)lngHandle;
				int lngBufferSize46 = 1;
				num3 = 1;
				main.FT_Write_Bytes(lngHandle47, ref txbyte, lngBufferSize46, ref num3);
				int loopcount = 0;
				bool loopuntilack = false;
				while (!loopuntilack)
				{
					FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
					j = 0;
					while (rxqueue == 0 & j < 100)
					{
						Thread.Sleep(50);
						FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
						j++;
					}
					int num22 = 1;
					int num23 = rxqueue;
					for (j = num22; j <= num23; j++)
					{
						int lngHandle48 = (int)lngHandle;
						int lngBufferSize47 = 1;
						num3 = 1;
						main.FT_Read_Bytes(lngHandle48, ref rxbyte, lngBufferSize47, ref num3);
						if ((int)rxbyte == ACK)
						{
							loopuntilack = true;
						}
					}
					if ((int)rxbyte == NAK)
					{
						MyProject.Forms.K8FlashStatus.fmode.ForeColor = Color.Orange;
						Thread.Sleep(200);
						txbyte = 80;
						int lngHandle49 = (int)lngHandle;
						int lngBufferSize48 = 1;
						num3 = 1;
						main.FT_Write_Bytes(lngHandle49, ref txbyte, lngBufferSize48, ref num3);
						FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
						j = 0;
						while (rxqueue == 0 & j < 10)
						{
							Thread.Sleep(50);
							FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
							j++;
						}
						int num24 = 1;
						int num25 = rxqueue;
						for (j = num24; j <= num25; j++)
						{
							int lngHandle50 = (int)lngHandle;
							int lngBufferSize49 = 1;
							num3 = 1;
							main.FT_Read_Bytes(lngHandle50, ref rxbyte, lngBufferSize49, ref num3);
						}
						Thread.Sleep(200);
						txbyte = 117;
						int lngHandle51 = (int)lngHandle;
						int lngBufferSize50 = 1;
						num3 = 1;
						main.FT_Write_Bytes(lngHandle51, ref txbyte, lngBufferSize50, ref num3);
						FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
						j = 0;
						while (rxqueue == 0 & j < 10)
						{
							Thread.Sleep(50);
							FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
							j++;
						}
						int num26 = 1;
						int num27 = rxqueue;
						for (j = num26; j <= num27; j++)
						{
							int lngHandle52 = (int)lngHandle;
							int lngBufferSize51 = 1;
							num3 = 1;
							main.FT_Read_Bytes(lngHandle52, ref rxbyte, lngBufferSize51, ref num3);
						}
						Thread.Sleep(200);
						txbyte = 32;
						int lngHandle53 = (int)lngHandle;
						int lngBufferSize52 = 1;
						num3 = 1;
						main.FT_Write_Bytes(lngHandle53, ref txbyte, lngBufferSize52, ref num3);
						FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
						txbyte = 0;
						int lngHandle54 = (int)lngHandle;
						int lngBufferSize53 = 1;
						num3 = 1;
						main.FT_Write_Bytes(lngHandle54, ref txbyte, lngBufferSize53, ref num3);
						FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
						txbyte = (byte)block;
						int lngHandle55 = (int)lngHandle;
						int lngBufferSize54 = 1;
						num3 = 1;
						main.FT_Write_Bytes(lngHandle55, ref txbyte, lngBufferSize54, ref num3);
						FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
						txbyte = 208;
						int lngHandle56 = (int)lngHandle;
						int lngBufferSize55 = 1;
						num3 = 1;
						main.FT_Write_Bytes(lngHandle56, ref txbyte, lngBufferSize55, ref num3);
						Thread.Sleep(200);
					}
					unchecked
					{
						if (loopcount > 100)
						{
							Interaction.MsgBox("No ACK after erasing a block=" + Conversion.Str(block) + " Programming aborted, reset ecu and reprogram", MsgBoxStyle.OkOnly, null);
							MyProject.Forms.K8FlashStatus.Close();
							this.B_FlashECU.Enabled = true;
							FT_status = (long)main.FT_ClrDtr(checked((int)lngHandle));
							FT_status = (long)main.FT_Close(checked((int)lngHandle));
							CommonFunctions.BlockPgm = true;
							return;
						}
						totaltime = DateTime.Now.Subtract(starttime);
						MyProject.Forms.K8FlashStatus.L_elapsedtime.Text = Conversions.ToString(totaltime.Minutes) + ":" + Conversions.ToString(totaltime.Seconds);
						MyProject.Forms.K8FlashStatus.L_elapsedtime.Text = Conversions.ToString(DateAndTime.TimeOfDay);
						MyProject.Forms.K8FlashStatus.Progressbar_Flashstatus.Value = loopcount;
						MyProject.Forms.K8FlashStatus.Progressbar_Flashstatus.Refresh();
						MyProject.Forms.K8FlashStatus.Refresh();
						Application.DoEvents();
					}
					loopcount++;
				}
				rxqueue = 0;
				j = 0;
				MyProject.Forms.K8FlashStatus.fmode.ForeColor = Color.Black;
				int page = 0;
				for (;;)
				{
					totaltime = DateTime.Now.Subtract(starttime);
					MyProject.Forms.K8FlashStatus.L_elapsedtime.Text = Conversions.ToString(totaltime.Minutes) + ":" + Conversions.ToString(totaltime.Seconds);
					MyProject.Forms.K8FlashStatus.Progressbar_Flashstatus.Value = page;
					MyProject.Forms.K8FlashStatus.Progressbar_Flashstatus.Refresh();
					MyProject.Forms.K8FlashStatus.Refresh();
					Application.DoEvents();
					j = 0;
					int y = 0;
					do
					{
						buff[y] = (byte)CommonFunctions.ReadFlashByte(block * 65536 + page * 256 + y);
						if (buff[y] != 255)
						{
							j++;
						}
						y++;
					}
					while (y <= 255);
					if (j > 0)
					{
						loopcount = 0;
						loopuntilack = false;
						while (!loopuntilack)
						{
							txbyte = 65;
							int lngHandle57 = (int)lngHandle;
							int lngBufferSize56 = 1;
							num3 = 1;
							main.FT_Write_Bytes(lngHandle57, ref txbyte, lngBufferSize56, ref num3);
							txbyte = (byte)page;
							int lngHandle58 = (int)lngHandle;
							int lngBufferSize57 = 1;
							num3 = 1;
							main.FT_Write_Bytes(lngHandle58, ref txbyte, lngBufferSize57, ref num3);
							txbyte = (byte)block;
							int lngHandle59 = (int)lngHandle;
							int lngBufferSize58 = 1;
							num3 = 1;
							main.FT_Write_Bytes(lngHandle59, ref txbyte, lngBufferSize58, ref num3);
							y = 0;
							do
							{
								txbyte = buff[y];
								y++;
							}
							while (y <= 255);
							int ftHandle = (int)lngHandle;
							byte[] lpBuffer = buff;
							int nBufferSize = 256;
							num3 = 256;
							unchecked
							{
								FT_status = (long)main.FT_Write(ftHandle, lpBuffer, nBufferSize, ref num3);
								rxbyte = 0;
								FT_status = (long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat);
								j = 0;
							}
							while (rxqueue == 0 & j < 30)
							{
								Thread.Sleep(25);
								FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
								if (rxqueue > 0)
								{
									j = 30;
								}
								j++;
							}
							int num28 = 1;
							int num29 = rxqueue;
							for (j = num28; j <= num29; j++)
							{
								int lngHandle60 = (int)lngHandle;
								int lngBufferSize59 = 1;
								num3 = 1;
								main.FT_Read_Bytes(lngHandle60, ref rxbyte, lngBufferSize59, ref num3);
								if ((int)rxbyte == ACK)
								{
									loopuntilack = true;
								}
							}
							if (loopcount > 5)
							{
								MyProject.Forms.K8FlashStatus.fmode.ForeColor = Color.Orange;
								MyProject.Forms.K8FlashStatus.Refresh();
								Application.DoEvents();
								Thread.Sleep(100);
								txbyte = 117;
								int lngHandle61 = (int)lngHandle;
								int lngBufferSize60 = 1;
								num3 = 1;
								main.FT_Write_Bytes(lngHandle61, ref txbyte, lngBufferSize60, ref num3);
								FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
								int lngHandle62 = (int)lngHandle;
								int lngBufferSize61 = 1;
								num3 = 1;
								main.FT_Read_Bytes(lngHandle62, ref rxbyte, lngBufferSize61, ref num3);
								Thread.Sleep(100);
								rxbyte = 0;
								FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
								j = 0;
								while (rxqueue == 0 & j < 30)
								{
									Thread.Sleep(25);
									FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
									if (rxqueue > 0)
									{
										j = 30;
									}
									j++;
								}
								int num30 = 1;
								int num31 = rxqueue;
								for (j = num30; j <= num31; j++)
								{
									int lngHandle63 = (int)lngHandle;
									int lngBufferSize62 = 1;
									num3 = 1;
									main.FT_Read_Bytes(lngHandle63, ref rxbyte, lngBufferSize62, ref num3);
								}
								txbyte = 80;
								int lngHandle64 = (int)lngHandle;
								int lngBufferSize63 = 1;
								num3 = 1;
								main.FT_Write_Bytes(lngHandle64, ref txbyte, lngBufferSize63, ref num3);
								FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
								int lngHandle65 = (int)lngHandle;
								int lngBufferSize64 = 1;
								num3 = 1;
								main.FT_Read_Bytes(lngHandle65, ref rxbyte, lngBufferSize64, ref num3);
								Thread.Sleep(100);
								rxbyte = 0;
								FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
								j = 0;
								while (rxqueue == 0 & j < 30)
								{
									Thread.Sleep(25);
									FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
									if (rxqueue > 0)
									{
										j = 30;
									}
									j++;
								}
								int num32 = 1;
								int num33 = rxqueue;
								for (j = num32; j <= num33; j++)
								{
									int lngHandle66 = (int)lngHandle;
									int lngBufferSize65 = 1;
									num3 = 1;
									main.FT_Read_Bytes(lngHandle66, ref rxbyte, lngBufferSize65, ref num3);
								}
							}
							if (loopcount > 10)
							{
								goto Block_72;
							}
							loopcount++;
						}
					}
					page++;
					if (page > 255)
					{
						goto IL_19FB;
					}
				}
				Block_72:
				Interaction.MsgBox(string.Concat(new string[]
				{
					"No ACK after writing a block=",
					Conversion.Str(block),
					" page=",
					Conversion.Str(page),
					". Programming aborted, reset ecu and reprogram"
				}), MsgBoxStyle.OkOnly, null);
				MyProject.Forms.K8FlashStatus.Close();
				this.B_FlashECU.Enabled = true;
				unchecked
				{
					FT_status = (long)main.FT_ClrDtr(checked((int)lngHandle));
					FT_status = (long)main.FT_Close(checked((int)lngHandle));
					CommonFunctions.BlockPgm = true;
					return;
				}
			}
			IL_19FB:;
		}
		txbyte = 225;
		int lngHandle67 = (int)lngHandle;
		int lngBufferSize66 = 1;
		num3 = 1;
		main.FT_Write_Bytes(lngHandle67, ref txbyte, lngBufferSize66, ref num3);
		txbyte = 0;
		int lngHandle68 = (int)lngHandle;
		int lngBufferSize67 = 1;
		num3 = 1;
		main.FT_Write_Bytes(lngHandle68, ref txbyte, lngBufferSize67, ref num3);
		txbyte = 0;
		int lngHandle69 = (int)lngHandle;
		int lngBufferSize68 = 1;
		num3 = 1;
		main.FT_Write_Bytes(lngHandle69, ref txbyte, lngBufferSize68, ref num3);
		txbyte = byte.MaxValue;
		int lngHandle70 = (int)lngHandle;
		int lngBufferSize69 = 1;
		num3 = 1;
		main.FT_Write_Bytes(lngHandle70, ref txbyte, lngBufferSize69, ref num3);
		txbyte = 15;
		int lngHandle71 = (int)lngHandle;
		int lngBufferSize70 = 1;
		num3 = 1;
		main.FT_Write_Bytes(lngHandle71, ref txbyte, lngBufferSize70, ref num3);
		Thread.Sleep(200);
		FT_status = unchecked((long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat));
		if (rxqueue != 2)
		{
			Interaction.MsgBox("Error in reading checksum from ecu", MsgBoxStyle.OkOnly, null);
		}
		int lngHandle72 = (int)lngHandle;
		int lngBufferSize71 = 1;
		num3 = 1;
		main.FT_Read_Bytes(lngHandle72, ref rxbyte, lngBufferSize71, ref num3);
		i = (int)rxbyte;
		int lngHandle73 = (int)lngHandle;
		int lngBufferSize72 = 1;
		num3 = 1;
		main.FT_Read_Bytes(lngHandle73, ref rxbyte, lngBufferSize72, ref num3);
		i += (int)rxbyte * 256;
		main.timeEndPeriod(1);
		if (i != 23205)
		{
			Interaction.MsgBox("Checksum error when validating the flash, please reflash your ecu before using it.", MsgBoxStyle.OkOnly, null);
			MyProject.Forms.K8FlashStatus.fmode.Text = "Checksum error, please reprogram";
			CommonFunctions.ResetBlocks();
			CommonFunctions.BlockPgm = true;
		}
		else
		{
			MyProject.Forms.K8FlashStatus.fmode.Text = "Flash OK, turn switch to enginedata";
			CommonFunctions.ResetBlocks();
			CommonFunctions.BlockPgm = false;
		}
		totaltime = DateTime.Now.Subtract(starttime);
		MyProject.Forms.K8FlashStatus.L_elapsedtime.Text = Conversions.ToString(totaltime.Minutes) + ":" + Conversions.ToString(totaltime.Seconds);
		MyProject.Forms.K8FlashStatus.Refresh();
		Application.DoEvents();
	}
	FT_status = (long)main.FT_ClrDtr(checked((int)lngHandle));
	Thread.Sleep(100);
	MyProject.Forms.K8FlashStatus.CloseEnabled = true;
	FT_status = (long)main.FT_GetModemStatus(checked((int)lngHandle), ref modemstat);
	while (modemstat == 24576 | modemstat == 25088)
	{
		Application.DoEvents();
		if (MyProject.Forms.K8FlashStatus.ClosedStatus)
		{
			break;
		}
		FT_status = (long)main.FT_GetStatus(checked((int)lngHandle), ref rxqueue, ref txqueue, ref eventstat);
		Thread.Sleep(200);
		modemstat = 0;
		FT_status = (long)main.FT_GetModemStatus(checked((int)lngHandle), ref modemstat);
	}
	MyProject.Forms.K8FlashStatus.Close();
	this.B_FlashECU.Enabled = true;
	FT_status = (long)main.FT_Close(checked((int)lngHandle));
	if (FT_status == 0L)
	{
		if (MyProject.Forms.K8Datastream.Visible)
		{
			MyProject.Forms.K8Datastream.startenginedatacomms();
		}
	}
	else
	{
		Interaction.MsgBox("Can not close com port, please save the bin and reboot your computer and reflash just in case.", MsgBoxStyle.OkOnly, null);
	}
	string flashfile = "ecuflash.bin";
	string binpath = Application.StartupPath;
	string path = binpath + flashfile;
	if (File.Exists(path))
	{
		File.Delete(path);
	}
	this.fs = File.Open(path, FileMode.CreateNew);
	this.fs.Write(CommonFunctions.Flash, 0, 1048576);
	this.fs.Close();
	string binfile = this.ECUID.Text + "-";
	binfile = binfile + Conversions.ToString(MyProject.Computer.Clock.LocalTime.Date.Day) + "-";
	binfile = binfile + Conversions.ToString(MyProject.Computer.Clock.LocalTime.Date.Month) + "-";
	binfile = binfile + Conversions.ToString(MyProject.Computer.Clock.LocalTime.Date.Year) + "-";
	binfile = binfile + Conversions.ToString(MyProject.Computer.Clock.LocalTime.Hour) + "-";
	binfile += Conversions.ToString(MyProject.Computer.Clock.LocalTime.Minute);
	binfile += ".bin";
	path = binpath + binfile;
	if (File.Exists(path))
	{
		File.Delete(path);
	}
	this.fs = File.Open(path, FileMode.CreateNew);
	this.fs.Write(CommonFunctions.Flash, 0, 1048576);
	this.fs.Close();
	this.CleanUpFDTDirectory(binpath);
}
