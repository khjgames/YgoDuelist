Detailing Cardpack Visuals Creation 

All of these elements are bundled under a node in the scene tree, it has an aespect ratio of 600 x 846.

First we need to create the background for the cardpack.
@YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_Background.png



// start tag number specific visuals

    if this pack has exactly 1 tag: [

        We need to create a darkened tint overlayed on top of the background, it is a white image that we need to recolor according to tag 1's given color, then apply that color to the image with Blend Mode: Darken, using @YgoDuelist/Game_Design/Darken_Shader.md 

        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_S_Tag_Darken_Tint.png 
    ]

    if this pack has exactly 2 tags: [

        We need to create a darkened tint overlayed on top of the background, it is a white image that we need to recolor according to tag 1's given color, then apply that color to the image with Blend Mode: Darken, using @YgoDuelist/Game_Design/Darken_Shader.md 
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_D_Tag_1_Darken_Tint.png

        We need to create another darkened tint overlayed on top of the background, it is a white image that we need to recolor according to tag 2's given color, then apply that color to the image with Blend Mode: Darken, using @YgoDuelist/Game_Design/Darken_Shader.md 
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_D_Tag_2_Darken_Tint.png 

        Next we need to create the Outline for the Cardpack, overlayed on top of the background.
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_D_Outline.png 
    ]

    if this pack has exactly 3 tags: [

        We need to create a darkened tint overlayed on top of the background, it is a white image that we need to recolor according to tag 1's given color, then apply that color to the image with Blend Mode: Darken, using @YgoDuelist/Game_Design/Darken_Shader.md 
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_T_Tag_1_Darken_Tint.png

        We need to create another darkened tint overlayed on top of the background, it is a white image that we need to recolor according to tag 2's given color, then apply that color to the image with Blend Mode: Darken, using @YgoDuelist/Game_Design/Darken_Shader.md 
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_T_Tag_2_Darken_Tint.png 

        Next, we need to create the Rift for the Cardpack, overlayed on top of the background.
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_T_Rift.png 

        Next we need to create the Outline for the Cardpack, overlayed on top of the background.
        @YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_T_Outline.png 
    ]

// end of tag number specific visuals



Lastly we need to create the Outline for the Cardpack, overlayed on top of the background.
@YgoDuelist/YgoDuelist/images/card_frames/Sealed_Cardpacks/Cardpack_Background_Outline.png