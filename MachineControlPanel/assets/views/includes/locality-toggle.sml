<frame left-click=|ToggleLocality()| focusable="true"
  background={@mushymato.MachineControlPanel/sprites/cursors:insetBg}
  padding="6"
  *switch={Locality}>
  <icon *case="Global" sprite={@mushymato.MachineControlPanel/sprites/furniture:globe} note={#machine-select.scope.everywhere}/>
  <icon *case="PerLocation" sprite={@mushymato.MachineControlPanel/sprites/objects:fence} note={#machine-select.scope.current-location}/>
  <icon *case="PerMachine" sprite={@mushymato.MachineControlPanel/sprites/objects:computer} note={#machine-select.scope.machine}/>
</frame>

<template name="icon">
  <lane orientation="horizontal">
    <image sprite={&sprite} layout="48px 48px" +hover:scale="1.14"/>
    <label text={&note}  margin="4,8" layout="content[140..] 32px"/>
  </lane>
</template>
