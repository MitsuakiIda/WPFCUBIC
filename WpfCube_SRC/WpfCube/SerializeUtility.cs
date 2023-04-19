using System;
using System.Collections.Generic;
using System.Windows;
using System.Text;
using System.Xaml;
using System.Xml;
using System.Windows.Markup;
using System.IO;

namespace emanual.Wpf.Utility
{
	public class SerializeUtility
	{
		//---------------------------------------------------------------------------------------------
		// 指定の WPF オブジェクトをシリアル化し、指定のファイルに出力する
		// obj : シリアル化するオブジェクト
		// fileName: 出力ファイル名（文字コードは UTF-8）
		public static void Serialize(object obj, string fileName)
		{
			string text = Serialize(obj);

			var sw = new StreamWriter(fileName, false, System.Text.Encoding.UTF8);

			try
			{
				sw.WriteLine(text);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			finally
			{
				sw.Close();
			}
		}

		//---------------------------------------------------------------------------------------------
		// 指定の WPF オブジェクトをシリアル化する
		// obj : シリアル化するオブジェクト
		public static string Serialize(object obj)
		{
			var settings = new XmlWriterSettings();

			// 出力時の条件
			settings.Indent = true;
			settings.NewLineOnAttributes = false;

			// XML バージョン情報の出力を抑制する
			settings.ConformanceLevel = ConformanceLevel.Fragment;

			var sb = new StringBuilder();
			XmlWriter writer = XmlWriter.Create(sb, settings);

			var manager = new XamlDesignerSerializationManager(writer);
			manager.XamlWriterMode = XamlWriterMode.Expression;

			try
			{
				System.Windows.Markup.XamlWriter.Save(obj, manager);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}

			return sb.ToString();
		}

		//---------------------------------------------------------------------------------------------
		// 指定のファイルから XAML 文を読み込んで逆シリアル化し、WPF オブジェクトを返す
		// fileName : XAML 文を保持するファイル名
		// encoding : 文字のエンコーディング法（null のとき、UTF-8）
		// 戻り値   : WPF オブジェクト
		public static object Deserialize(string fileName, System.Text.Encoding encoding)
		{
			string text = String.Empty;
			StreamReader sr;

			if (encoding == null)
				sr = new StreamReader(fileName, System.Text.Encoding.UTF8);
			else
				sr = new StreamReader(fileName, encoding);

			try
			{
				text = sr.ReadToEnd();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			finally
			{
				sr.Close();
			}

			object obj = Deserialize(text);

			return obj;
		}

		//---------------------------------------------------------------------------------------------
		// 指定の XAML 文を読み込んで逆シリアル化し、WPF オブジェクトを返す
		// xamlText : XAML 文
		// 戻り値   : WPF オブジェクト
		public static object Deserialize(string xamlText)
		{
			var doc = new XmlDocument();

			try
			{
				doc.LoadXml(xamlText);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}

			object obj = null;

			try
			{
				obj = System.Windows.Markup.XamlReader.Load(new XmlNodeReader(doc));
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}

			return obj;
		}

	} // end of SerializeUtility class
} // end of namespace
