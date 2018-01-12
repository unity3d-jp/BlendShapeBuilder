file(GLOB sources *.cpp *.h)
set(plugins_dir "${CMAKE_SOURCE_DIR}/../BlendShapeBuilder/Assets/UTJ/VertexTweaker/Plugins/x86_64")
add_plugin(VertexTweakerCore SOURCES ${sources} PLUGINS_DIR ${plugins_dir})

add_dependencies(VertexTweakerCore MeshUtils)
target_link_libraries(VertexTweakerCore MeshUtils ${EXTERNAL_LIBS})
install(TARGETS VertexTweakerCore DESTINATION .)
