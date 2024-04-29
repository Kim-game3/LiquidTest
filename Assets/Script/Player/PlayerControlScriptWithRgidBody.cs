﻿//
// Mecanimのアニメーションデータが、原点で移動しない場合の Rigidbody付きコントローラ
// サンプル
// 2014/03/13 N.Kobyasahi
//
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

namespace UnityChan
{
    // 必要なコンポーネントの列記
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]

    public class PlayerControlScriptWithRgidBody : MonoBehaviour
    {

        public float animSpeed = 1.5f;              // アニメーション再生速度設定
        public float lookSmoother = 3.0f;           // a smoothing setting for camera motion
        public bool useCurves = true;               // Mecanimでカーブ調整を使うか設定する
                                                    // このスイッチが入っていないとカーブは使われない
        public float useCurvesHeight = 0.5f;        // カーブ補正の有効高さ（地面をすり抜けやすい時には大きくする）

        // 以下キャラクターコントローラ用パラメタ
        public static float player_speed = 20.0f;
        [SerializeField]
        [Header("プレイヤーの速度")]
        float speed;
        //下降速度
        [SerializeField] float down_speed;
        // ジャンプ威力
        public static float jumpPower = 6.0f;
        //ジャンプ可能か
        public static bool enabled_jump = true;
        // キャラクターコントローラ（カプセルコライダ）の参照
        private CapsuleCollider col;
        private Rigidbody rb;
        // キャラクターコントローラ（カプセルコライダ）の移動量
        public static Vector3 velocity;
        // CapsuleColliderで設定されているコライダのHeiht、Centerの初期値を収める変数
        private float orgColHight;
        private Vector3 orgVectColCenter;
        private Animator anim;                          // キャラにアタッチされるアニメーターへの参照
        private AnimatorStateInfo currentBaseState;         // base layerで使われる、アニメーターの現在の状態の参照

        //private GameObject cameraObject;    // メインカメラへの参照

        // アニメーター各ステートへの参照
        static int idleState = Animator.StringToHash("Base Layer.Idle");
        static int locoState = Animator.StringToHash("Base Layer.Locomotion");
        static int jumpState = Animator.StringToHash("Base Layer.Jump");
        static int restState = Animator.StringToHash("Base Layer.Rest");

        [SerializeField] SkinnedMeshRenderer mesh;
        enum Status { Water,Ice,Gas};
        [SerializeField] int status;
        // 初期化
        void Start()
        {
            // Animatorコンポーネントを取得する
            anim = GetComponent<Animator>();
            // CapsuleColliderコンポーネントを取得する（カプセル型コリジョン）
            col = GetComponent<CapsuleCollider>();
            rb = GetComponent<Rigidbody>();
            //メインカメラを取得する
            //cameraObject = GameObject.FindWithTag("MainCamera");
            // CapsuleColliderコンポーネントのHeight、Centerの初期値を保存する
            orgColHight = col.height;
            orgVectColCenter = col.center;

            enabled_jump = true;
            isInvincible = false;
            player_speed = speed;
        }

        void gas_update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {   // スペースキーを入力したら

                //アニメーションのステートがLocomotionの最中のみジャンプできる
                if (currentBaseState.fullPathHash == locoState )
                {
                    //ステート遷移中でなかったらジャンプできる

                }

                if (!anim.IsInTransition(0) && enabled_jump)
                {
                    rb.AddForce(Vector3.up * jumpPower, ForceMode.VelocityChange);
                    anim.SetBool("Jump", true);     // Animatorにジャンプに切り替えるフラグを送る
                    enabled_jump = false;
                }
            }
            rb.AddForce(Vector3.down * Time.deltaTime * down_speed, ForceMode.Acceleration);
            rb.useGravity = false;
        }

        void water_update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {   // スペースキーを入力したら

                ////アニメーションのステートがLocomotionの最中のみジャンプできる
                //if (currentBaseState.fullPathHash == locoState )
                //{
                //ステート遷移中でなかったらジャンプできる
                if (!anim.IsInTransition(0) && enabled_jump)
                {
                    rb.AddForce(Vector3.up * jumpPower, ForceMode.VelocityChange);

                    if (currentBaseState.fullPathHash == locoState)
                        anim.SetBool("Jump", true);   // Animatorにジャンプに切り替えるフラグを送る
                    enabled_jump = false;
                }
                //}
            }
            
        }

        private void Update()
        {
            switch (status)
            {
                case (int)Status.Water: water_update(); break;
                case (int)Status.Ice: water_update();break;
                case (int)Status.Gas:gas_update();break;
            }
        }


        // 以下、メイン処理.リジッドボディと絡めるので、FixedUpdate内で処理を行う.
        void FixedUpdate()
        {
            float h = Input.GetAxis("Horizontal");              // 入力デバイスの水平軸をhで定義
            float v = Input.GetAxis("Vertical");                // 入力デバイスの垂直軸をvで定義
            anim.SetFloat("Speed", v);                          // Animator側で設定している"Speed"パラメタにvを渡す
            anim.SetFloat("Direction", h);                      // Animator側で設定している"Direction"パラメタにhを渡す
            anim.speed = animSpeed;                             // Animatorのモーション再生速度に animSpeedを設定する
            currentBaseState = anim.GetCurrentAnimatorStateInfo(0); // 参照用のステート変数にBase Layer (0)の現在のステートを設定する


            //自由回転の処理
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            Screen_movement(mx, my);

            // 以下、キャラクターの移動処理
            velocity = new Vector3(h , 0, v);  // 上下のキー入力からZ軸方向の移動量を取得

            if (status != (int)Status.Gas)
                rb.useGravity = true;//ジャンプ中に重力を切るので、それ以外は重力の影響を受けるようにする
                                 // キャラクターのローカル空間での方向に変換
            velocity = transform.TransformDirection(velocity);

            //Debug.Log("速さ:" + velocity.magnitude * player_speed);
            velocity = velocity.normalized * player_speed;


            // 上下のキー入力でキャラクターを移動させる
            transform.localPosition += velocity * Time.fixedDeltaTime;

            // 左右のキー入力でキャラクタをY軸で旋回させる
            //transform.Rotate(0, h * rotateSpeed, 0);


            // 以下、Animatorの各ステート中での処理
            // Locomotion中
            // 現在のベースレイヤーがlocoStateの時
            if (currentBaseState.fullPathHash == locoState)
            {
                //カーブでコライダ調整をしている時は、念のためにリセットする
                if (useCurves)
                {
                    resetCollider();
                }
            }
            
            // IDLE中の処理
            // 現在のベースレイヤーがidleStateの時
            else if (currentBaseState.fullPathHash == idleState)
            {
                //カーブでコライダ調整をしている時は、念のためにリセットする
                if (useCurves)
                {
                    resetCollider();
                }
                // スペースキーを入力したらRest状態になる
                if (Input.GetButtonDown("Jump"))
                {
                    //anim.SetBool("Rest", true);
                }

            }
            // REST中の処理
            // 現在のベースレイヤーがrestStateの時
            else if (currentBaseState.fullPathHash == restState)
            {
                //cameraObject.SendMessage("setCameraPositionFrontView");		// カメラを正面に切り替える
                // ステートが遷移中でない場合、Rest bool値をリセットする（ループしないようにする）
                if (!anim.IsInTransition(0))
                {
                    anim.SetBool("Rest", false);
                }

            }

            // JUMP中の処理
            // 現在のベースレイヤーがjumpStateの時
            else if (currentBaseState.fullPathHash == jumpState)
            {
                //cameraObject.SendMessage ("setCameraPositionJumpView");	// ジャンプ中のカメラに変更
                // ステートがトランジション中でない場合
                if (!anim.IsInTransition(0))
                {

                    // 以下、カーブ調整をする場合の処理
                    if (useCurves)
                    {
                        // 以下JUMP00アニメーションについているカーブJumpHeightとGravityControl
                        // JumpHeight:JUMP00でのジャンプの高さ（0〜1）
                        // GravityControl:1⇒ジャンプ中（重力無効）、0⇒重力有効
                        float jumpHeight = anim.GetFloat("JumpHeight");
                        float gravityControl = anim.GetFloat("GravityControl");
                        if (gravityControl > 0)
                            rb.useGravity = false;  //ジャンプ中の重力の影響を切る

                        // レイキャストをキャラクターのセンターから落とす
                        Ray ray = new Ray(transform.position + Vector3.up, -Vector3.up);
                        RaycastHit hitInfo = new RaycastHit();
                        // 高さが useCurvesHeight 以上ある時のみ、コライダーの高さと中心をJUMP00アニメーションについているカーブで調整する
                        if (Physics.Raycast(ray, out hitInfo))
                        {
                            if (hitInfo.distance > useCurvesHeight)
                            {
                                col.height = orgColHight - jumpHeight;          // 調整されたコライダーの高さ
                                float adjCenterY = orgVectColCenter.y + jumpHeight;
                                col.center = new Vector3(0, adjCenterY, 0); // 調整されたコライダーのセンター
                            }
                            else
                            {
                                // 閾値よりも低い時には初期値に戻す（念のため）					
                                resetCollider();
                            }
                        }
                    }
                    // Jump bool値をリセットする（ループしないようにする）				
                    anim.SetBool("Jump", false);
                }
            }
        }

        void OnGUI()
        {
            //GUI.Box (new Rect (Screen.width - 260, 10, 250, 150), "Interaction");
            //GUI.Label (new Rect (Screen.width - 245, 30, 250, 30), "Up/Down Arrow : Go Forwald/Go Back");
            //GUI.Label (new Rect (Screen.width - 245, 50, 250, 30), "Left/Right Arrow : Turn Left/Turn Right");
            //GUI.Label (new Rect (Screen.width - 245, 70, 250, 30), "Hit Space key while Running : Jump");
            //GUI.Label (new Rect (Screen.width - 245, 90, 250, 30), "Hit Spase key while Stopping : Rest");
            //GUI.Label (new Rect (Screen.width - 245, 110, 250, 30), "Left Control : Front Camera");
            //GUI.Label (new Rect (Screen.width - 245, 130, 250, 30), "Alt : LookAt Camera");
        }


        // キャラクターのコライダーサイズのリセット関数
        void resetCollider()
        {
            // コンポーネントのHeight、Centerの初期値を戻す
            col.height = orgColHight;
            col.center = orgVectColCenter;
        }

        IEnumerator Blink()
        {
            for (float j = 0; j < invincibly_time; j += 0.4f)
            {
                for (int i = 0; i < 100; i++)
                {
                    mesh.material.color = mesh.material.color - new Color32(0, 0, 0, 1);
                }

                yield return new WaitForSeconds(0.2f);

                for (int k = 0; k < 100; k++)
                {
                    mesh.material.color = mesh.material.color + new Color32(0, 0, 0, 1);
                }

                yield return new WaitForSeconds(0.2f);
            }
        }

        public static float invincibly_time = 3.0f;
        public static bool isInvincible = false;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "Floor")
            {
                enabled_jump = true;
            }

            if (!isInvincible)
            {

                if (collision.gameObject.tag == "Boss" || collision.gameObject.tag == "Enemy")
                {
                    StartCoroutine(Blink());
                    PlayerHp.Damage(1000);
                    Debug.Log("Blink");
                    isInvincible = true;
                    Invoke("end_invincible", invincibly_time);
                    //rb.AddForce(transform.TransformDirection(Vector3.back * 5.0f));
                }
            }
        }

        void end_invincible()
        {
            isInvincible = false;
        }

        [Header("カメラの回転スピード(水平方向)")]
        [SerializeField] float x_speed;
        void Screen_movement(float mx,float my)
        {
            // X方向に一定量移動していれば横回転
            //0.0000001fは滑らかさ
            if (Mathf.Abs(mx) > 0.0000001f)
            {
                mx = mx * x_speed * Time.fixedDeltaTime;

                // 回転軸はワールド座標のY軸
                transform.RotateAround(transform.position, Vector3.up, mx);
            }
        }
    }
}