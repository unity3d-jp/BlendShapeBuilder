# BlendShapeBuilder
[English](https://translate.google.com/translate?sl=ja&tl=en&u=https://github.com/unity3d-jp/BlendShapeBuilder) (by Google Translate)  

Unity 上で頂点数が同じモデルを合成して blend shape 化するツールです。
  
blend shape を持った fbx を生成できないモデリングツールは、Maya などのツールを経由して生成する必要がありますが、それを Unity 上でできるようにすることで手順を簡略化しようというものです。  
また、頂点位置だけ、法線だけの合成といった特殊な blend shape ターゲットも生成でき、既存の blend shape をターゲットごとに別個の Mesh として書き出す機能なども備えています。  
Unity 2017.1 系以上で動作を確認しています。

## 使い方
- [BlendShapeBuilder.unitypackage](https://github.com/unity3d-jp/BlendShapeBuilder/releases/download/20171228/BlendShapeBuilder.unitypackage) をプロジェクトにインポート
- Window メニューに "Blend Shape Builder" と "Blend Shape Inspector" が追加されるので、それらでツールウィンドウを開く

![](https://user-images.githubusercontent.com/1488611/34403838-0c4361ae-ebee-11e7-85e2-5e7b13d89eeb.png)

Base Mesh やターゲットとなるオブジェクトは、Mesh アセットもしくは MeshRenderer か SkinnedMeshRenderer を持つ GameObject を設定します。

- "Base Mesh" には元となる Mesh を指定します。  
"Find Targets" ボタンを押すと、現在のシーン内にある Base Mesh と同じ頂点数のモデルを探して選択状態にします。

- "BlendShapes" にモーフターゲットとなるオブジェクトを指定していきます。  
"▼ BlendShapes" の部分にオブジェクトを Drag & Drop すると、放り込んだオブジェクト毎に BlendShape が生成されます。  
各 BlendShape のフォールド部分 ("▼ NewBlendShape0" など) にオブジェクトを Drag & Drop すると、放り込んだオブジェクトがその BlendShape のフレームとして登録されます。

- V,N,T のチェックは Vertex, Normal, Tangent の略で、その要素を含めるかの指定になります。例えば Normal のみをチェックした場合、頂点の移動はせず法線だけが変わる BlendShape になります。

- "Update Mesh" ボタンを押すと、Base Mesh に指定した Mesh を直接更新します。この処理は Undo できないので注意が必要です。  
"Preserve Existing BlendShapes" をチェックしておくと、既存の BlendShape は持ち越しつつ追加します。(同名の BlendShape があった場合新しい方で上書きされます)

- "Generate New Mesh" ボタンを押すと、Base Mesh には手を加えず、新しい Mesh を生成してそれに結果を出力します。

- "Export BaseMesh To .asset" を押すと Base Mesh に指定した Mesh を asset ファイルとして保存します。"Generate New Mesh" で生成した Mesh はそのままではそのシーン内でしか使えず、他のシーンで使用できるようにするためにはこのコマンドでアセット化する必要があります。

## 注意点
Blend Shape のターゲットは頂点の数と順番が元モデルと一致している必要があります。しかし、DCC ツール上では一致していても Unity にインポートする際に変換処理によって変わってしまうことがあります。  
この変化は主に以下のような場合に起こりえます：

- 不連続な法線 (=ハードエッジ) がある
- UV がモデル毎に異なり、かつ不連続な UV がある
- 面毎に違うマテリアルを割り当てている

例えばメタセコイアの場合、これを回避するには、スムージング の 角度 を 180 にしてハードエッジを回避し、全モデルで同じ UV を使用し (そもそも Unity の Blend Shape は UV の変化はサポートしていません)、マテリアルは 1 オブジェクトにつき 1 種のみにする必要があります。

## License
[MIT](LICENSE.txt)
