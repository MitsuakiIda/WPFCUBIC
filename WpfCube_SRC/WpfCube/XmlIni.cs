/*  XML 形式の INI ファイル（初期化ファイル）を操作するクラス
 * 
 *  たいていのアプリケーションはカスタマイズ、つまり、ユーザーの好みに設定する機能を
 *  持っている。かつては、INI ファイルを使っていたが、Windows 95 の頃に Windows レジストリに
 *  カスタマイズ情報を格納するようになった。.NET Framework の時代は、XML ドキュメントの使用が
 *  提唱されている。このクラスは、INI ファイルを扱うようにカスタマイズ情報を格納し、データを
 *  読み込む機能を持つ。フォームやコントロールとのバインディングの機能はないが、実用的に
 *  使えるはずである。
 * 
 * 
 *  （TypeConverter の都合で設定可能なデータ型）
 *  bool	byte	char	Color	Cursor		DateTime	decimal	double	Font	float	Guid
 *  short	int		long	Point	Rectangle	Size			SizeF
 * 
 *  （TypeConverter の都合で設定できないデータ型）
 *  PointF	RectangleF	
 * 
 *  本クラスはフリーウエアである。コードの改変・流用は無制限に許可する。
 *  なお、このクラスは .WPF0 対応である。
 *
 *  2010/03/09
 *  佐藤 正
 *  e-mail   : pmansato@kanazawa-net.ne.jp
 *  HomePage : http://www.kanazawa-net.ne.jp/~pmansato/
 */

using System;
using System.Collections.Generic; // List<T>

using System.Xml;
using System.Xml.XPath; // XPathException
using System.IO; // File. Path
using System.ComponentModel; // TypeDescriptor
using System.Windows;

namespace emanual.Wpf.Utility
{
	public class XmlIni
	{
		private XmlDocument FDocument; // XML ドキュメントオブジェクト
		private string      FFileName; // INI ファイル名
		private string      FExeName;  // アプリケーションの exe ファイル名

		// ------------------------------------------------------------------
		// コンストラクタ
		// fileName : INI ファイル名（任意の名前でよいが、.xmlini を推奨）
		public XmlIni(string fileName)
		{
			FFileName = fileName;
			System.Reflection.Assembly a = System.Reflection.Assembly.GetEntryAssembly();
			FExeName = a.Location;
			FDocument = new XmlDocument();

			SetDeclarationAndComment();

			if (File.Exists(FFileName))
				FDocument.Load(FFileName);
		}

		// ------------------------------------------------------------------
		// .xmlini ファイルに保存したフォームの位置とサイズを読み込んでフォームに設定する
		// form : 対象のフォーム
		// left, top, width, height : 初めて起動するときのデフォルトの位置とサイズ
		// 通常はフォームの Load イベント内で呼び出す
		public void SetFormPositionAndSize(Window form, string sectionName, string keyName,
				int left, int top, int width, int height)
		{
			Rect defaultValue = new Rect(left, top, width, height);

			try
			{
				// 絶対パスを使ってセクションノードを取得する
				XmlNode sectionNode = FDocument.SelectSingleNode("/root/" + sectionName);

				// セクションがあるとき
				if (sectionNode != null)
				{
					// sectionNode からの相対パスでキーノードを取得する
					XmlNode keyNode = sectionNode.SelectSingleNode(keyName);

					// キーがあるとき
					if (keyNode != null)
					{
						TypeConverter converter = TypeDescriptor.GetConverter(typeof(Rect));
						defaultValue = (Rect)converter.ConvertFromString(keyNode.InnerText);
					}
				}

				// フォームの位置とサイズを設定する
				//form.Bounds = defaultValue;
				form.Left = defaultValue.Left;
				form.Top = defaultValue.Top;
				form.Width = defaultValue.Width;
				form.Height = defaultValue.Height;
			}
			catch (XPathException ex)
			{
				MessageBox.Show(ex.Message, "XPathException");
			}
			catch (XmlException ex)
			{
				MessageBox.Show(ex.Message, "XmlException");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Exception");
			}
		}

		// ------------------------------------------------------------------
		// フォームを破棄するときにその位置とサイズを .xmlini ファイルに保存する
		// 最大化あるいはアイコン化した状態で終了しても通常表示時の
		// 位置とサイズを記録するため、RestoreBounds プロパティを利用する
		// 通常はフォームの FormClosed イベント内で呼び出す
		public void WriteFormPositionAndSize(Window form, string sectionName, string keyName)
		{
			// セクションとキーの指定が不適切なとき（null か、空の文字列のとき）
			if (CheckSectionAndKeyName(sectionName, keyName) == false)
				return;

			TypeConverter converter = TypeDescriptor.GetConverter(typeof(Rect));

			// フォームの通常表示時の位置とサイズを返す RestoreBounds プロパティを使う
			// これは最小化または最大化した状態のときにフォームを閉じる場合に備えたものである
			string valueText;

			valueText = (string)converter.ConvertToString(form.RestoreBounds);

			// 絶対パスを使ってセクションノードを取得する
			XmlNode sectionNode = FDocument.SelectSingleNode("/root/" + sectionName);

			// 指定のセクションが存在しないとき（当然、キーもないので、両方を作成する）
			if (sectionNode == null)
			{
				XmlElement sectionElement = FDocument.CreateElement(sectionName);
				XmlElement keyElement = FDocument.CreateElement(keyName);
				XmlText textNode = FDocument.CreateTextNode(valueText);

				XmlNode node = FDocument.DocumentElement.AppendChild(sectionElement);
				XmlNode keyNode = node.AppendChild(keyElement);
				keyNode.AppendChild(textNode);
			}
			else
			{
				XmlElement keyElement = sectionNode[keyName];

				if (keyElement == null) // セクションはあるが、キーはないとき
				{
					XmlElement key = FDocument.CreateElement(keyName);
					XmlText textNode = FDocument.CreateTextNode(valueText);

					XmlNode keyNode = sectionNode.AppendChild(key);
					keyNode.AppendChild(textNode);
				}
				else // セクションもキーもあるときは、値だけを変更する
				{
					keyElement.InnerText = valueText;
				}
			}

			FDocument.Save(FFileName);
			FDocument.Load(FFileName);
		}

		// ------------------------------------------------------------------
		// 指定のキーを削除する
		public void DeleteKey(string sectionName, string keyName)
		{
			try
			{
				// 絶対パスでセクションノードを取得する
				XmlNode sectionNode = FDocument.SelectSingleNode("/root/" + sectionName);

				// sectionNode からの相対パスでキーノードを取得する
				XmlNode keyNode = sectionNode.SelectSingleNode(keyName);

				sectionNode.RemoveChild(keyNode);

				FDocument.Save(FFileName);
				FDocument.Load(FFileName);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "DeleteKey()");
			}
		}

		// ------------------------------------------------------------------
		// 指定のセクションのすべてのキーを削除する
		//public void DeleteAllKeys(string sectionName)
		//{
		//  int count = this.GetKeyCount(sectionName);

		//  if (count < 1)
		//    return;

		//  string[] keyNames = new string[count];

		//  keyNames = this.GetKeyNames(sectionName);

		//  try
		//  {
		//    for (int i = 0; i < keyNames.Length; ++i)
		//    {
		//      // 絶対パスでセクションノードを取得する
		//      XmlNode sectionNode = FDocument.SelectSingleNode("/root/" + sectionName);

		//      // sectionNode からの相対パスでキーノードを取得する
		//      XmlNode keyNode = sectionNode.SelectSingleNode(keyNames[i]);

		//      sectionNode.RemoveChild(keyNode);
		//    }

		//    FDocument.Save(FFileName);
		//    FDocument.Load(FFileName);
		//  }
		//  catch (Exception ex)
		//  {
		//    MessageBox.Show(ex.Message, "DeleteAllKeys()");
		//  }
		//}

		// ------------------------------------------------------------------
		// 指定のセクションを削除する
		public void DeleteSection(string sectionName)
		{
			// 絶対パスでセクションノードを取得する
			XmlNode sectionNode = FDocument.SelectSingleNode("/root/" + sectionName);

			// 指定のセクションが存在しないとき
			if (sectionNode == null)
				return;

			FDocument.DocumentElement.RemoveChild(sectionNode);

			FDocument.Save(FFileName);
			FDocument.Load(FFileName);
		}

		// ------------------------------------------------------------------
		// 指定のセクションが存在するかどうかをチェックする
		public bool SectionExists(string sectionName)
		{
			XmlNode sectionNode = FDocument.SelectSingleNode("/root/" + sectionName);

			if (sectionNode != null)
				return true;
			else
				return false;
		}

		//*****************************************************************
		// Read ～ メソッド
		//*****************************************************************

		// 指定のキーの値を取得する
		// キーが存在しないときは defaultValue を返す
		public object ReadValue(string sectionName, string keyName, object defaultValue)
		{
			Type type = defaultValue.GetType();
			object result = defaultValue;

			try
			{
				XmlNode sectionNode = FDocument.SelectSingleNode("/root/" + sectionName);

				// セクションがあるとき
				if (sectionNode != null)
				{
					// sectionNode からの相対パスでキーノードを取得する
					XmlNode keyNode = sectionNode.SelectSingleNode(keyName);

					// キーがあるとき
					if (keyNode != null)
					{
						TypeConverter converter = TypeDescriptor.GetConverter(type);
						result = converter.ConvertFromString(keyNode.InnerText);
					}
				}
			}
			catch (XPathException ex)
			{
				MessageBox.Show(ex.Message, "XPathException");
			}
			catch (XmlException ex)
			{
				MessageBox.Show(ex.Message, "XmlException");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Exception");
			}

			return result;
		}

		// ------------------------------------------------------------------
		// 指定のセクションの中にあるすべてのキー名を取得する
		public List<string> ReadKeys(string sectionName)
		{
			XmlNode sectionNode = FDocument.SelectSingleNode("/root/" + sectionName);

			List<string> result = new List<string>();

			if (sectionNode != null)
			{
				XmlNodeList list = sectionNode.ChildNodes;

				foreach (XmlNode node in list)
				{
					result.Add(node.Name);
				}
			}

			return result;
		}

		// ------------------------------------------------------------------
		// すべてのセクション名を取得する
		public List<string> ReadSections()
		{
			XmlNodeList list = FDocument.DocumentElement.ChildNodes;

			List<string> result = new List<string>(list.Count);

			for (int i = 0; i < list.Count; ++i)
			{
				result.Add(list[i].Name);
			}

			return result;
		}

		//*****************************************************************
		// Write ～ メソッド
		//*****************************************************************

		// 指定のキーに object 型値を設定する
		// このメソッドを一度でも呼び出すと、.xmlini ファイルを作成する
		public void WriteValue(string sectionName, string keyName, object value)
		{
			// セクションとキーの指定が不適切なとき（null か、空の文字列のとき）
			if (CheckSectionAndKeyName(sectionName, keyName) == false)
				return;

			Type type = value.GetType();
			TypeConverter converter = TypeDescriptor.GetConverter(type);

			string valueText = (string)converter.ConvertToString(value);

			// 絶対パスを使ってセクションノードを取得する
			XmlNode sectionNode = FDocument.SelectSingleNode("/root/" + sectionName);

			// 指定のセクションが存在しないとき（当然、キーもないので、両方を作成する）
			if (sectionNode == null)
			{
				XmlElement sectionElement = FDocument.CreateElement(sectionName);
				XmlElement keyElement = FDocument.CreateElement(keyName);
				XmlText textNode = FDocument.CreateTextNode(valueText);

				XmlNode node = FDocument.DocumentElement.AppendChild(sectionElement);
				XmlNode keyNode = node.AppendChild(keyElement);
				keyNode.AppendChild(textNode);
			}
			else
			{
				XmlElement keyElement = sectionNode[keyName];

				if (keyElement == null) // セクションはあるが、キーはないとき
				{
					XmlElement key = FDocument.CreateElement(keyName);
					XmlText textNode = FDocument.CreateTextNode(valueText);

					XmlNode keyNode = sectionNode.AppendChild(key);
					keyNode.AppendChild(textNode);
				}
				else // セクションもキーもあるときは、値だけを変更する
				{
					keyElement.InnerText = valueText;
				}
			}

			FDocument.Save(FFileName);

			FDocument.Load(FFileName);
		}

		//*****************************************************************
		// private メソッド
		//*****************************************************************

		// .xmlini ファイルを作成し、XML 宣言部、コメント部、ルート要素を設定する
		// すでにあれば、何もしない
		// ルート要素名は常に <root> ～ </root> である
		private void SetDeclarationAndComment()
		{
			if (!File.Exists(FFileName))
			{
				StreamWriter writer = new StreamWriter(FFileName);

				writer.WriteLine("<?xml version=\"1.0\" encoding=\"shift-jis\"?>");
				writer.WriteLine("<!--Initialization File for " + FExeName + "-->");
				writer.WriteLine("<root>");
				writer.WriteLine("</root>");

				writer.Close();
				return;
			}
		}

		// ------------------------------------------------------------------
		// セクション名とキー名の設定があるかどうかをチェックする
		// null、空の文字列、途中にスペースを含むものは使えない
		// セクション名とキー名の最初の文字として、数字は使えない
		private bool CheckSectionAndKeyName(string sectionName, string keyName)
		{
			bool result = false;

			if ((sectionName != null) && (keyName != null))
			{
				if ((sectionName.Length >= 1) && (keyName.Length >= 1))
					result = true;
			}
	
			// sectionName の途中にスペースを含むかどうかをチェックする
			for (int i = 0; i < sectionName.Length; ++i)
			{
				if (sectionName[i] == ' ')
				{
					result = false;
					break;
				}
			}

			// keyName の途中にスペースを含むかどうかをチェックする
			for (int i = 0; i < keyName.Length; ++i)
			{
				if (keyName[i] == ' ')
				{
					result = false;
					break;
				}
			}

			// 先頭の文字が数字かどうかをチェックする
			if (sectionName[0] >= '0' && sectionName[0] <= '9')
				result = false;

			if (keyName[0] >= '0' && keyName[0] <= '9')
				result = false;

			return result;
		}
	
	} // end of XmlIni class
}// end of namespace
