# Blend Shape Builder & Vertex Tweaker
[English](https://translate.google.com/translate?sl=ja&tl=en&u=https://github.com/unity3d-jp/BlendShapeBuilder) (by Google Translate)  

Unity 上で blend shape を作成するツールです。既存のモデルの合成はもちろん、Unity 上で頂点を編集してそれを blend shape 化することもできます。  
また、頂点の位置だけ、法線だけの合成といった特殊な blend shape ターゲットも生成でき、既存の blend shape をターゲットごとに別個の Mesh として書き出す機能なども備えています。  
Unity 2017.1 系以上で動作を確認しています。

## 使い方
- [BlendShapeBuilder.unitypackage](https://github.com/unity3d-jp/BlendShapeBuilder/releases/download/20171228/BlendShapeBuilder.unitypackage) をプロジェクトにインポート
- Window メニューに "Blend Shape Builder" と "Blend Shape Inspector" と "Vertex Tweaker" が追加されます。  
Blend Shape Builder が blend shape をオーサリングするツール、Vertex Tweaker が頂点の編集を行うツール、Blend Shape Inspector は既存の blend shape を調べたりデータを抽出したりするツールです。 



## Blend Shape Builder
Blend Shape Builder のウィンドウを開いた状態で MeshRenderer もしくは SkinnedMeshRenderer を持つオブジェクトを選択すると "Add BlendShapeBuilder" というボタンが出てくるので、それでコンポーネントを追加します。  


blend shape のターゲットとなるオブジェクトは、Mesh アセットもしくは MeshRenderer か SkinnedMeshRenderer を持つ GameObject を設定します。(頂点数が同じである必要があります)
- "Find Targets" ボタンを押すと、現在のシーン内にある Base Mesh と同じ頂点数のモデルを探して選択状態にします。

- "BlendShapes" にモーフターゲットとなるオブジェクトを指定していきます。
  - 既存の Mesh をターゲットに登録したい場合、それらを Drag & Drop します。
"▼ BlendShapes" の部分にオブジェクトを Drag & Drop すると、放り込んだオブジェクト毎に BlendShape が生成されます。  
各 BlendShape のフォールド部分 ("▼ NewBlendShape0" など) にオブジェクトを Drag & Drop するとその BlendShape のフレームとして登録されます。

  - "+" ボタンでフレームを追加すると、フレームが追加されると共にそれに対応する Mesh が生成されます。

  - "Edit" を押すと、そのフレームの Mesh の編集に移行します。(後述の Vertex Tweaker が開きます)

  - V,N,T のチェックは Vertex, Normal, Tangent の略で、その要素を含めるかの指定になります。例えば Normal のみをチェックした場合、頂点の移動はせず法線だけが変わる BlendShape になります。

- "Update Mesh" を押すと現在の Mesh を直接更新します。
"Generate New Asset" は現在の Mesh には手を加えず、新しいアセットとしてエクスポートします。
"Preserve Existing BlendShapes" をチェックしておくと、Mesh が既に blend shape を持っていた場合にそれを持ち越しつつ追加します。(同名の BlendShape があった場合新しい方で上書きします)  


## Vertex Tweaker
頂点を編集するツールです。Skinning された Mesh の編集もサポートしており、blend shape 用に限らずモデルを微調整したい場合全般に使えます。ただし、できるのはあくまで頂点の移動のみであり、頂点の増減など (いわゆるトポロジーが変化する操作) はできません。

- Edit -> Move, Rotate, Scale はその名の通り頂点の移動、回転、拡縮を行うモードです

- Edit -> Assign はいわゆる数値入力です。XYZ 軸別にマスクをかけられるので、例えば球体の下半分を選択し Y だけチェックして 0 を入力することで半球にする、といった使い方ができます。

- Edit -> Projection は他のモデルに対して頂点の投影を行うモードです。各頂点からレイを飛ばし、対象モデルに当たった地点に移動させます。頂点数が全く合わないモデルに対するモーフィングを実現できます。

- Select は頂点の選択に関するオプションです。矩形選択、投げ縄選択、ブラシ選択といった一般的な選択方法や、選択中の頂点と繋がった頂点郡 (Connected)、ポリゴンの切れ目上にある頂点 (Edge, Hole) の選択なども備えています。

- Misc -> Mirroring で方向を選択すると、ミラーリングが有効になります。(対称なモデルである必要があります)
- Misc -> Normals および Tangents は法線と接線の再計算のオプションです。手動で編集した法線があってそれを維持したい場合、"Manual" に変えて自動計算は行わないようにする必要があるでしょう。そうでない場合はデフォルトで問題ないはずです。

- Shift を押しながらの選択は選択の足し算、Ctrl を押しながらだと選択の引き算になります。

## 注意点
- インポートした Mesh に対する編集  
  fbx ファイルなどからインポートした Mesh はプロジェクトを開くたびに再生成が行われます。このため、インポートした Mesh に対する編集は、そのままではプロジェクトを開き直すとリセットされてしまいます。  
  これを回避するには Mesh を独立したアセットに変換する必要があります。
  Blend Shape Builder の "Generate New Asset"、もしくは Vertex Tweaker の "Export -> Export .asset" はこのためのコマンドで、編集中のモデルを独立したアセットとしてエクスポートします。

- DCC ツール上では一致していた頂点数が Unity 上では一致しない場合  
  Blend Shape のターゲットは頂点の数と順番が元モデルと一致している必要があります。しかし、DCC ツール上では一致していても Unity にインポートする際に変換処理によって変わってしまうことがあります。これは主に以下のような場合に起こりえます：
  - 不連続な法線 (=ハードエッジ) がある
  - UV がモデル毎に異なり、かつ不連続な UV がある
  - 面毎に違うマテリアルを割り当てている
  
  例えばメタセコイアの場合、これを回避するには、スムージング の 角度 を 180 にしてハードエッジを回避し、全モデルで同じ UV を使用し (そもそも Unity の Blend Shape は UV の変化はサポートしていません)、マテリアルは 1 オブジェクトにつき 1 種のみにする必要があります。
  
## 関連ツール
- [NormalPainter](https://github.com/unity3d-jp/NormalPainter) - 法線を編集するツールです。
- [FbxExporter](https://github.com/unity3d-jp/FbxExporter) - Mesh を fbx 形式でエクスポートするツールです。skinning や blend shape もサポートしています。

## License
[MIT](LICENSE.txt)
