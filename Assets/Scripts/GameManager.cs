using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance
    {
        get => _instance;
    }
    private static GameManager _instance = null;

    public enum ePuyoMode
    {
        TokoPuyo,
        PuyoZu,
    }

    public enum eUpdateState
    {
        Stop,
        // PlayState
        Play, // 自由落下

        Check, // 消えるかチェック（＋少しのウェイト）
        Vanish, // 消える
        Drop, // 落ちる

        Pause,

        Num,
    }

    public static int COLUMN_NUM = 6;
    public static int ROW_NUM = 13;
    public static float TILE_SIZE = 75f;

    private System.Action[] startStateEvents = null;
    private System.Action[] updateStateEvents = null;
    private System.Action[] endStateEvents = null;

    public ePuyoMode puyoMode = ePuyoMode.PuyoZu;
    public eUpdateState updateState = eUpdateState.Stop;

    public Transform puyoParent = null;
    public GameObject puyoPrefab = null;
    public TextController rensaText = null;
    public TextController modeChangeText = null;
    public PuyoDispController nextPuyoDispController = null;
    public PuyoDispController nextNextPuyoDispController = null;

    private bool isExistVanishingPuyo = false;
    private float waitTimer = 0.0f;
    private int rensaNum = 0;

    private List<PuyoController> puyoList = new List<PuyoController>();
    private List<PuyoStatusInfo> prevPuyoList = new List<PuyoStatusInfo>();
    private PuyoController selectedPalletePuyo = null;

    private const int BUFFER_NUM = 65536;
    private PuyoDropInfo[] dropPuyoArr = new PuyoDropInfo[BUFFER_NUM];
    private int BufferIndex = 0;
    private PuyoController CurrentDropPuyo = null;

    void Awake()
    {
        if (_instance != null)
        {
            Component.Destroy(this);
            return;
        }
        _instance = this;

        startStateEvents = new System.Action[(int)eUpdateState.Num]
        {
            StartStopState,
            StartPlayState,
            StartCheckState,
            StartVanishState,
            StartDropState,
            StartPauseState,
        };
        updateStateEvents = new System.Action[(int)eUpdateState.Num]
        {
            UpdateStopState,
            UpdatePlayState,
            UpdateCheckState,
            UpdateVanishState,
            UpdateDropState,
            UpdatePauseState,
        };
        endStateEvents = new System.Action[(int)eUpdateState.Num]
        {
            EndStopState,
            EndPlayState,
            EndCheckState,
            EndVanishState,
            EndDropState,
            EndPauseState,
        };
    }

    void Start()
    {
        rensaText.SetText($"<b>{0}</b>");
        // プヨ配列を生成
        SetupDropPuyo();
    }

    void Update()
    {
        updateStateEvents[(int)updateState].Invoke();
    }

    private void ChangeState(eUpdateState state)
    {
        endStateEvents[(int)updateState].Invoke();
        updateState = state;
        startStateEvents[(int)updateState].Invoke();
    }

    #region UpdateState
    #region End
    /// <summary>
    /// 停止中
    /// </summary>
    private void StartStopState()
    {

    }
    /// <summary>
    /// 停止中
    /// </summary>
    private void UpdateStopState()
    {

    }
    /// <summary>
    /// 停止中
    /// </summary>
    private void EndStopState()
    {
        rensaNum = 0;
    }
    #endregion

    #region Play
    /// <summary>
    /// プレイ中（操作可能）
    /// </summary>
    private void StartPlayState()
    {
        waitTimer = 0.0f;
        if (puyoMode == ePuyoMode.TokoPuyo)
        {
            CurrentDropPuyo = CreatePuyo(dropPuyoArr[BufferIndex]);
            var next_index = (BufferIndex + 1) % BUFFER_NUM;
            var next_next_index = (BufferIndex + 2) % BUFFER_NUM;
            BufferIndex = (BufferIndex + 1) % BUFFER_NUM;

            nextPuyoDispController.SetPuyo(dropPuyoArr[next_index]);
            nextNextPuyoDispController.SetPuyo(dropPuyoArr[next_next_index]);
        }
    }

    /// <summary>
    /// プレイ中（操作可能）
    /// </summary>
    private void UpdatePlayState()
    {
        if (puyoMode == ePuyoMode.TokoPuyo)
        {
            if (CurrentDropPuyo.IsPuted())
            {
                ChangeState(eUpdateState.Drop);
            }
        }
    }

    /// <summary>
    /// プレイ中（操作可能）
    /// </summary>
    private void EndPlayState()
    {
        rensaNum = 0;
    }
    #endregion

    #region Check
    /// <summary>
    /// 消えるかチェック＋ウェイト
    /// </summary>
    private void StartCheckState()
    {
        List<PuyoController> vanish_list = new List<PuyoController>();
        waitTimer = 0.0f;
        isExistVanishingPuyo = false;

        for(int i=0; i<puyoList.Count; ++i)
        {
            if (puyoList[i].isReqVanish)
            {
                continue;
            }
            if (vanish_list.Contains(puyoList[i]))
            {
                continue;
            }

            List<PuyoController> new_vanish_list = new List<PuyoController>();
            // 四方に同じ色があるかどうかチェック
            CheckVanishNeighborhoodPuyo(puyoList[i], ref new_vanish_list);
            // 4つ以上つながっている場合は消えるフラグをオンにする
            if (new_vanish_list.Count >= 4)
            {
                isExistVanishingPuyo = true;
                for(int j=0; j<new_vanish_list.Count; ++j)
                {
                    if (vanish_list.Contains(new_vanish_list[j]) == false)
                    {
                        vanish_list.Add(new_vanish_list[j]);
                    }
                }
            }
        }

        for(int i=0; i<vanish_list.Count; ++i)
        {
            vanish_list[i].RequestVanish();
        }
    }

    /// <summary>
    /// 消えるかチェック＋ウェイト
    /// </summary>
    private void UpdateCheckState()
    {
        waitTimer += Time.deltaTime;
        if (waitTimer < 0.2f)
        {
            return;
        }
        // 消えるところがあるか
        if (isExistVanishingPuyo)
        {
            ChangeState(eUpdateState.Vanish);
        }
        // 消えない時はPlayへ
        else
        {
            // スキップモードの時は次に落ちてくるものがないので停止
            if (puyoMode == ePuyoMode.PuyoZu)
            {
                ChangeState(eUpdateState.Stop);
            }
            else
            {
                ChangeState(eUpdateState.Play);
            }
        }
    }

    /// <summary>
    /// 消えるかチェック＋ウェイト
    /// </summary>
    private void EndCheckState()
    {

    }
    #endregion

    #region Vanish
    /// <summary>
    /// 消える＋ウェイト
    /// </summary>
    private void StartVanishState()
    {
        waitTimer = 0.0f;
        rensaNum++;
        rensaText.SetText($"<b>{rensaNum}</b>");

        // 消えるフラグがついてるやつは消す
        for(int i=0; i<puyoList.Count; ++i)
        {
            if (puyoList[i].isReqVanish)
            {
                RemovePuyo(puyoList[i], false);
            }
        }
        puyoList.RemoveAll((x) => { return x.isReqVanish; });
    }

    /// <summary>
    /// 消える＋ウェイト
    /// </summary>
    private void UpdateVanishState()
    {
        waitTimer += Time.deltaTime;
        if (waitTimer < 0.2f)
        {
            return;
        }
        ChangeState(eUpdateState.Drop);
    }

    /// <summary>
    /// 消える＋ウェイト
    /// </summary>
    private void EndVanishState()
    {

    }
    #endregion

    #region Drop
    /// <summary>
    /// 落下＋ウェイト
    /// </summary>
    private void StartDropState()
    {
        waitTimer = 0.0f;

        // 下にあるやつから順に並び替える
        puyoList.Sort((x, y) => { return (x.position.y * 100 + x.position.x) - (y.position.y * 100 + y.position.x); });


        // 下にあるやつから一番下まで下ろす
        for(int i=0; i<puyoList.Count; ++i)
        {
            while(puyoList[i].isFallable())
            {
                var pos = puyoList[i].position;
                pos.y -= 1;
                puyoList[i].position = pos;
            }
        }
    }

    /// <summary>
    /// 落下＋ウェイト
    /// </summary>
    private void UpdateDropState()
    {
        waitTimer += Time.deltaTime;
        if (waitTimer < 0.2f)
        {
            return;
        }
        ChangeState(eUpdateState.Check);
    }

    /// <summary>
    /// 落下＋ウェイト
    /// </summary>
    private void EndDropState()
    {
    }
    #endregion

    #region Pause
    /// <summary>
    /// 一時停止中
    /// </summary>
    private void StartPauseState()
    {

    }

    /// <summary>
    /// 一時停止中
    /// </summary>
    private void UpdatePauseState()
    {

    }

    /// <summary>
    /// 一時停止中
    /// </summary>
    private void EndPauseState()
    {

    }
    #endregion
    #endregion

    /// <summary>
    /// 降ってくるぷよ配列を生成
    /// </summary>
    private void SetupDropPuyo()
    {
        var diff = System.DateTime.Now - new System.DateTime(2000, 1, 1, 0, 0, 0);
        uint rand = (uint)diff.TotalSeconds;
        // ABCDの順に入れて並び替える
        List<PuyoController.ePuyoType> puyoList = new List<PuyoController.ePuyoType>();
        for(int i=0; i<BUFFER_NUM*2; ++i)
        {
            puyoList.Add((PuyoController.ePuyoType)((i % 4) + (int)PuyoController.ePuyoType.Red));
        }
        dropPuyoArr = new PuyoDropInfo[BUFFER_NUM];
        for (int i=0; i<BUFFER_NUM; ++i)
        {
            rand = GetNextRand(rand);
            int rand_index = (int)(rand % puyoList.Count);
            var puyo1 = puyoList[rand_index];
            puyoList.RemoveAt(rand_index);

            rand = GetNextRand(rand);
            rand_index = (int)(rand % puyoList.Count);
            var puyo2 = puyoList[rand_index];
            puyoList.RemoveAt(rand_index);

            var info = new PuyoDropInfo()
            {
                puyoType1 = puyo1,
                puyoType2 = puyo2,
            };
            dropPuyoArr[i] = info;
        }
    }

    private uint GetNextRand(uint rand)
    {
        return (rand * 0x5D588B65 + 0x269EC3) & 0xFFFFFFFF;
    }

    private void CheckVanishNeighborhoodPuyo(PuyoController puyo, ref List<PuyoController> vanish_list)
    {
        // 既に消える予定のやつはチェックしない
        if (puyo.isReqVanish)
        {
            return;
        }
        // 既にチェック済みのやつはチェックしない
        if (vanish_list.Contains(puyo))
        {
            return;
        }
        #region ローカル関数

        PuyoController check(int add_x, int add_y)
        {
            var _pos = puyo.position;
            var _check_pos = _pos;
            _check_pos.x += add_x;
            _check_pos.y += add_y;
            var _check_puyo = GetPuyo(_check_pos);
            if (_check_puyo != null && _check_puyo.puyoType == puyo.puyoType && _check_puyo.isReqVanish == false)
            {
                return _check_puyo;
            }
            return null;
        }
        #endregion

        // 上
        var up = check(0, 1);
        if (up != null)
        {
            if (vanish_list.Contains(puyo) == false)
            {
                vanish_list.Add(puyo);
            }
            CheckVanishNeighborhoodPuyo(up, ref vanish_list);
        }
        // 下
        var down = check(0, -1);
        if (down != null)
        {
            if (vanish_list.Contains(puyo) == false)
            {
                vanish_list.Add(puyo);
            }
            CheckVanishNeighborhoodPuyo(down, ref vanish_list);
        }
        // 左
        var left = check(-1, 0);
        if (left != null)
        {
            if (vanish_list.Contains(puyo) == false)
            {
                vanish_list.Add(puyo);
            }
            CheckVanishNeighborhoodPuyo(left, ref vanish_list);
        }
        // 右
        var right = check(1, 0);
        if (right != null)
        {
            if (vanish_list.Contains(puyo) == false)
            {
                vanish_list.Add(puyo);
            }
            CheckVanishNeighborhoodPuyo(right, ref vanish_list);
        }
    }

    /// <summary>
    /// ぷよを登録
    /// </summary>
    public void RegisterPuyo(PuyoController puyo)
    {
        if (puyoList.Contains(puyo) == false)
        {
            puyoList.Add(puyo);
        }
    }

    /// <summary>
    /// 配置情報からぷよを取得
    /// </summary>
    /// <returns></returns>
    public PuyoController GetPuyo(Vector2Int position)
    {
        return puyoList.Find((x) => x.position == position);
    }

    /// <summary>
    /// 選択中ぷよを設定
    /// </summary>
    /// <param name="puyo"></param>
    public void SetSelectPuyo(PuyoController puyo)
    {
        if (selectedPalletePuyo != null)
        {
            selectedPalletePuyo.OnUnselected();
        }
        selectedPalletePuyo = puyo;
        selectedPalletePuyo.OnSelected();
    }

    /// <summary>
    /// 選択中ぷよを取得
    /// </summary>
    /// <returns></returns>
    public PuyoController GetSelectPuyo()
    {
        return selectedPalletePuyo;
    }

    /// <summary>
    /// パレットのぷよをフィールドに配置
    /// </summary>
    public void SetPalletePuyo(Vector2Int position)
    {
        if (puyoMode == ePuyoMode.TokoPuyo)
        {
            return;
        }
        var puyo = GetPuyo(position);
        if (selectedPalletePuyo != null && selectedPalletePuyo.puyoType != PuyoController.ePuyoType.None)
        {
            // 既存ぷよがある場合はぷよを上書き
            if (puyo != null)
            {
                puyo.Change(selectedPalletePuyo);
            }
            // ない場合はプヨを作成
            else
            {
                RegisterPuyo(CreatePuyo(selectedPalletePuyo, position));
            }
        }
        else
        {
            if (puyo != null)
            {
                RemovePuyo(puyo, true);
            }
        }
    }

    /// <summary>
    /// ぷよ作成
    /// </summary>
    /// <param name="pallete_puyo"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public PuyoController CreatePuyo(PuyoController pallete_puyo, Vector2Int position)
    {
        var new_puyo_obj = GameObject.Instantiate(puyoPrefab, Vector3.zero, Quaternion.identity, puyoParent);
        var new_puyo = new_puyo_obj.GetComponent<PuyoController>();
        new_puyo.puyoType = pallete_puyo.puyoType;
        new_puyo.Setup();
        new_puyo.position = position;
        return new_puyo;
    }

    /// <summary>
    /// ぷよ作成
    /// </summary>
    /// <param name="pallete_puyo"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public PuyoController CreatePuyo(PuyoStatusInfo puyo_info)
    {
        var new_puyo_obj = GameObject.Instantiate(puyoPrefab, Vector3.zero, Quaternion.identity, puyoParent);
        var new_puyo = new_puyo_obj.GetComponent<PuyoController>();
        new_puyo.puyoType = puyo_info.puyoType;
        new_puyo.Setup();
        new_puyo.position = puyo_info.position;
        return new_puyo;
    }

    /// <summary>
    /// ぷよ作成
    /// </summary>
    /// <param name="pallete_puyo"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public PuyoController CreatePuyo(PuyoDropInfo puyo_info)
    {
        var new_puyo_obj_1 = GameObject.Instantiate(puyoPrefab, Vector3.zero, Quaternion.identity, puyoParent);
        var new_puyo_1 = new_puyo_obj_1.GetComponent<PuyoController>();
        new_puyo_1.puyoType = puyo_info.puyoType1;
        new_puyo_1.Setup();
        new_puyo_1.position = new Vector2Int(2, 12);

        var new_puyo_obj_2 = GameObject.Instantiate(puyoPrefab, Vector3.zero, Quaternion.identity, puyoParent);
        var new_puyo_2 = new_puyo_obj_2.GetComponent<PuyoController>();
        new_puyo_2.puyoType = puyo_info.puyoType2;
        new_puyo_2.Setup();
        new_puyo_2.SetParent(new_puyo_1);

        return new_puyo_1;
    }

    /// <summary>
    /// ぷよ削除
    /// </summary>
    /// <param name="puyo"></param>
    public void RemovePuyo(PuyoController puyo, bool isRemoveFromList)
    {
        if (puyo == null)
        {
            return;
        }
        if (isRemoveFromList)
        {
            if (puyoList.Contains(puyo))
            {
                puyoList.Remove(puyo);
            }
        }
        GameObject.Destroy(puyo.gameObject);
    }

    /// <summary>
    /// ぷよの状態を保存
    /// </summary>
    private void SavePrevPuyoList()
    {
        prevPuyoList.Clear();
        for(int i=0; i<puyoList.Count; ++i)
        {
            prevPuyoList.Add(new PuyoStatusInfo(puyoList[i]));
        }
    }

    /// <summary>
    /// ぷよの状態を復元
    /// </summary>
    private void RevertPuyoList()
    {
        ResetPuyoList();
        for(int i=0; i<prevPuyoList.Count; ++i)
        {
            RegisterPuyo(CreatePuyo(prevPuyoList[i]));
        }
    }

    /// <summary>
    /// ぷよを全消し
    /// </summary>
    private void ResetPuyoList()
    {
        for(int i=0; i<puyoList.Count; ++i)
        {
            RemovePuyo(puyoList[i], false);
        }
        puyoList.Clear();
    }

    /// <summary>
    /// ぷよモード切り替え
    /// </summary>
    public void OnClickChangeMode()
    {
        ePuyoMode nextMode = puyoMode;
        // ぷよ図のときは、stopのみ切り替え可能
        if (puyoMode == ePuyoMode.PuyoZu)
        {
            if (updateState == eUpdateState.Stop)
            {
                nextMode = ePuyoMode.TokoPuyo;
            }
        }
        else
        {
            if (updateState == eUpdateState.Stop || updateState == eUpdateState.Pause || updateState == eUpdateState.Play)
            {
                nextMode = ePuyoMode.PuyoZu;
            }
        }

        if (puyoMode == nextMode)
        {
            return;
        }

        puyoMode = nextMode;
        switch (puyoMode)
        {
            case ePuyoMode.PuyoZu:
                if (modeChangeText != null)
                {
                    modeChangeText.SetText("PUYO-ZU");
                }
                ChangeState(eUpdateState.Stop);
                if (CurrentDropPuyo != null)
                {
                    RemovePuyo(CurrentDropPuyo, true);
                    RemovePuyo(CurrentDropPuyo.parentPuyo, true);
                    RemovePuyo(CurrentDropPuyo.childPuyo, true);
                    BufferIndex = (BufferIndex - 1 + BUFFER_NUM) % BUFFER_NUM;
                    nextPuyoDispController.Remove();
                    nextNextPuyoDispController.Remove();
                }
                break;
            case ePuyoMode.TokoPuyo:
                if (modeChangeText != null)
                {
                    modeChangeText.SetText("TOKO-PUYO");
                }
                ChangeState(eUpdateState.Play);
                break;
        }
    }

    /// <summary>
    /// ぷよを右に移動
    /// </summary>
    public void OnClickMoveRight()
    {
        if (puyoMode == ePuyoMode.TokoPuyo)
        {
            if (CurrentDropPuyo != null)
            {
                var move = new Vector2Int(1, 0);
                if (CurrentDropPuyo.IsMove(move))
                {
                    CurrentDropPuyo.Move(move);
                }
            }
        }
    }

    /// <summary>
    /// ぷよを左に移動
    /// </summary>
    public void OnClickMoveLeft()
    {
        if (puyoMode == ePuyoMode.TokoPuyo)
        {
            if (CurrentDropPuyo != null)
            {
                var move = new Vector2Int(-1, 0);
                if (CurrentDropPuyo.IsMove(move))
                {
                    CurrentDropPuyo.Move(move);
                }
            }
        }
    }

    /// <summary>
    /// ぷよをクイックドロップ
    /// </summary>
    public void OnClickQuickDrop()
    {
        if (puyoMode == ePuyoMode.TokoPuyo)
        {
            if (CurrentDropPuyo != null)
            {
                CurrentDropPuyo.quickDrop();
            }
        }
    }

    /// <summary>
    /// ぷよを右回転
    /// </summary>
    public void OnClickRotateRight()
    {
        if (puyoMode == ePuyoMode.TokoPuyo)
        {
            if (CurrentDropPuyo != null)
            {
                CurrentDropPuyo.rotateRight();
            }
        }
    }

    /// <summary>
    /// ぷよを左回転
    /// </summary>
    public void OnClickRotateLeft()
    {
        if (puyoMode == ePuyoMode.TokoPuyo)
        {
            if (CurrentDropPuyo != null)
            {
                CurrentDropPuyo.rotateLeft();
            }
        }
    }

    /// <summary>
    /// プレイ
    /// </summary>
    public void OnClickPlay()
    {
        if (puyoMode == ePuyoMode.PuyoZu && updateState == eUpdateState.Stop)
        {
            SavePrevPuyoList();
            ChangeState(eUpdateState.Drop);
        }
    }

    /// <summary>
    /// 戻る
    /// </summary>
    public void OnClickRevert()
    {
        if (puyoMode == ePuyoMode.PuyoZu && updateState == eUpdateState.Stop)
        {
            RevertPuyoList();
        }
    }

    /// <summary>
    /// リセット
    /// </summary>
    public void OnClickReset()
    {
        if (puyoMode == ePuyoMode.PuyoZu && updateState == eUpdateState.Stop)
        {
            ResetPuyoList();
        }
    }
}

public class PuyoStatusInfo
{
    public PuyoController.ePuyoType puyoType = PuyoController.ePuyoType.None;
    public Vector2Int position = Vector2Int.zero;

    public PuyoStatusInfo(PuyoController puyo)
    {
        if (puyo != null)
        {
            puyoType = puyo.puyoType;
            position = puyo.position;
        }
    }
}

public class PuyoDropInfo
{
    public PuyoController.ePuyoType puyoType1;
    public PuyoController.ePuyoType puyoType2;
}
