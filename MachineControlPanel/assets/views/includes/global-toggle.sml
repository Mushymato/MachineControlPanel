<frame left-click=|ToggleGlobalLocal()| focusable="true"
  background={@mushymato.MachineControlPanel/sprites/cursors:insetBg}
  padding="6">
  <globe *!if={IsGlobal} tint="#00000088" note={#machine-select.scope.current-location}/>
  <globe *if={IsGlobal} tint="White" note={#machine-select.scope.everywhere}/>
</frame>

<template name="globe">
  <lane orientation="horizontal">
    <image 
      sprite={@mushymato.MachineControlPanel/sprites/furniture:globe}
      tint={&tint}
      layout="48px 48px"
      +hover:scale="1.14"
    />
    <label text={&note}  margin="4,8" layout="content[140..] 32px"/>
  </lane>
</template>
