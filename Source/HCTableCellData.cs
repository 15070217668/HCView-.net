﻿/*******************************************************}
{                                                       }
{               HCView V1.1  作者：荆通                 }
{                                                       }
{      本代码遵循BSD协议，你可以加入QQ群 649023932      }
{            来获取更多的技术交流 2018-5-4              }
{                                                       }
{            表格单元格内各类对象管理单元               }
{                                                       }
{*******************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HC.Win32;

namespace HC.View
{
    public delegate HCCustomData GetRootDataEventHandler();

    public delegate bool GetEnableUndoEventHandler();

    public class HCTableCellData : HCRichData
    {

        private bool FActive;

        // 标识单元格全选状态(全选时点击虽然没在内部Item上也应标识在选中区域)
        // 因CellData如果是EmptyData时，全选并没有SelectEndItem，获取其选中状态时
        // 和未全选一样，所以需要自己记录全选状态
        private bool FCellSelectedAll;

        private int FCellHeight;  // 所属单元格高度(因合并或手动拖高，单元格高度会大于等于其内数据高度)

        private GetRootDataEventHandler FOnGetRootData;

        private GetEnableUndoEventHandler FOnGetEnableUndo;

        private bool PointInCellRect(POINT APt)
        {
            return HC.PtInRect(HC.Bounds(0, 0, Width, FCellHeight), APt);
        }

        protected override int GetHeight()
        {
            int Result = base.GetHeight();
            if (this.DrawItems.Count > 0)
                Result += DrawItems[0].Rect.Top;

            return Result;
        }

        /// <summary> 取消选中 </summary>
        /// <returns>取消时当前是否有选中，True：有选中；False：无选中</returns>
        public override bool DisSelect()
        {
            bool Result = base.DisSelect();
            FCellSelectedAll = false;
            return Result;
        }

        /// <summary> 删除选中 </summary>
        public override bool DeleteSelected()
        {
            bool Result = base.DeleteSelected();
            FCellSelectedAll = false;
            return Result;
        }

        protected override void _FormatReadyParam(int AStartItemNo, ref int APrioDrawItemNo, ref POINT APos)
        {
            base._FormatReadyParam(AStartItemNo, ref APrioDrawItemNo, ref APos);
        }

        protected override bool EnableUndo()
        {
            if (FOnGetEnableUndo != null)
                return FOnGetEnableUndo();
            else
                return base.EnableUndo();
        }

        protected void SetActive(bool Value)
        {
            if (FActive != Value)
                FActive = Value;

            if (!FActive)
            {
                this.DisSelect();
                this.InitializeField();
                Style.UpdateInfoRePaint();
            }
        }

        public HCTableCellData(HCStyle AStyle)
            : base(AStyle)
        {

        }

        //constructor Create; override;
        /// <summary> 全选 </summary>
        public override void SelectAll()
        {
            base.SelectAll();
            FCellSelectedAll = true;
        }

        /// <summary> 坐标是否在AItem的选中区域中 </summary>
        public override bool CoordInSelect(int X, int Y, int  AItemNo, int AOffset, bool ARestrain)
        {
            if (FCellSelectedAll)
                return PointInCellRect(new POINT(X, Y));
            else
                return base.CoordInSelect(X, Y, AItemNo, AOffset, ARestrain);
        }

        /// <summary> 返回指定坐标下的Item和Offset </summary>
        public override void GetItemAt(int X, int Y, ref int AItemNo, ref int AOffset, ref int ADrawItemNo, ref bool ARestrain)
        {
            base.GetItemAt(X, Y, ref AItemNo, ref AOffset, ref ADrawItemNo, ref ARestrain);
            if (FCellSelectedAll)
                ARestrain = !PointInCellRect(new POINT(X, Y));
        }

        public override HCCustomData GetRootData()
        {
            if (FOnGetRootData != null)
                return FOnGetRootData();
            else
                return base.GetRootData();
        }

        /// <summary> 选在第一个Item最前面 </summary>
        public bool SelectFirstItemOffsetBefor()
        {
            bool  Result = false;
            if ((!SelectExists()) && (SelectInfo.StartItemNo == 0))
                Result = (SelectInfo.StartItemOffset == 0);

            return Result;
        }

        /// <summary> 选在最后一个Item最后面 </summary>
        public bool SelectLastItemOffsetAfter()
        {
            bool Result = false;
            if ((!SelectExists()) && (SelectInfo.StartItemNo == this.Items.Count - 1))
                Result = (SelectInfo.StartItemOffset == this.GetItemAfterOffset(SelectInfo.StartItemNo));

            return Result;
        }

        /// <summary> 选在第一行 </summary>
        public bool SelectFirstLine()
        {
            return (this.GetParaFirstItemNo(SelectInfo.StartItemNo) == 0);
        }

        /// <summary> 选在最后一行 </summary>
        public bool SelectLastLine()
        {
            return (this.GetParaLastItemNo(SelectInfo.StartItemNo) == this.Items.Count - 1);
        }

        /// <summary> 清除并返回为处理分页比净高增加的高度(为重新格式化时后面计算偏移用) </summary>
        public int ClearFormatExtraHeight()
        {
            int Result = 0;
            int vFmtOffset = 0;
            for (int i = 1; i <= DrawItems.Count - 1; i++)
            {
                if (DrawItems[i].LineFirst)
                {
                    if (DrawItems[i].Rect.Top != DrawItems[i - 1].Rect.Bottom)
                    {
                        vFmtOffset = DrawItems[i].Rect.Top - DrawItems[i - 1].Rect.Bottom;
                        if (vFmtOffset > Result)
                            Result = vFmtOffset;
                    }
                }

                HC.OffsetRect(ref DrawItems[i].Rect, 0, -vFmtOffset);
                if (Items[DrawItems[i].ItemNo].StyleNo < HCStyle.Null)
                {
                    int vFormatIncHight = (Items[DrawItems[i].ItemNo] as HCCustomRectItem).ClearFormatExtraHeight();
                    DrawItems[i].Rect.Bottom = DrawItems[i].Rect.Bottom - vFormatIncHight;
                }
            }

            return Result;
        }

        /// <summary> 单元格全先状态 </summary>
        public bool CellSelectedAll
        {
            get { return FCellSelectedAll; }
            set { FCellSelectedAll = value; }
        }

        /// <summary> 所属单元格高度 </summary>
        public int CellHeight
        {
            get { return FCellHeight; }
            set { FCellHeight = value; }
        }

        // 用于表格切换编辑的单元格
        public bool Active
        {
            get { return FActive; }
            set { SetActive(value); }
        }

        public GetRootDataEventHandler OnGetRootData
        {
            get { return FOnGetRootData; }
            set { FOnGetRootData = value; }
        }

        public GetEnableUndoEventHandler OnGetEnableUndo
        {
            get { return FOnGetEnableUndo; }
            set { FOnGetEnableUndo = value; }
        }
    }
}