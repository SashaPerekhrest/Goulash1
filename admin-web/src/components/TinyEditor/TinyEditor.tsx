import { Editor } from "@tinymce/tinymce-react";
import "tinymce/tinymce";
import "tinymce/models/dom";
import "tinymce/icons/default";
import "tinymce/themes/silver";
import "tinymce/plugins/autolink";
import "tinymce/plugins/image";
import "tinymce/plugins/link";
import "tinymce/plugins/lists";
import "tinymce/plugins/code";
import "tinymce/plugins/table";
import "tinymce/plugins/wordcount";
import "tinymce/skins/ui/oxide/skin.min.css";
import "tinymce/skins/content/default/content.min.css";
import { uploadAdminFile } from "../../shared/api/files";

interface TinyEditorProps {
  disabled?: boolean;
  id?: string;
  onChange: (value: string) => void;
  value: string;
}

export function TinyEditor({ disabled = false, id = "tiny-editor", onChange, value }: TinyEditorProps) {
  return (
    <Editor
      disabled={disabled}
      id={id}
      init={{
        branding: false,
        // Keep image URLs exactly as returned by the upload API (absolute with
        // the /portfolio/files/... prefix). Without this TinyMCE rewrites them
        // to relative paths like ../files/..., which break on pages of
        // different depth.
        convert_urls: false,
        relative_urls: false,
        content_css: false,
        content_style:
          "body { color: #20242a; font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; font-size: 16px; line-height: 1.65; margin: 1rem; } a { color: #2563eb; } img { height: auto; max-width: 100%; } table { border-collapse: collapse; margin: 1rem 0; max-width: 100%; width: 100%; } td, th { padding: 0.5rem; vertical-align: top; } body::after { clear: both; content: ''; display: block; }",
        formats: {
          alignleft: [
            { selector: "img", styles: { float: "left", margin: "0 1rem 1rem 0" } },
            { block: "p", styles: { textAlign: "left" } }
          ],
          aligncenter: [
            { selector: "img", styles: { display: "block", float: "none", marginLeft: "auto", marginRight: "auto" } },
            { block: "p", styles: { textAlign: "center" } }
          ],
          alignright: [
            { selector: "img", styles: { float: "right", margin: "0 0 1rem 1rem" } },
            { block: "p", styles: { textAlign: "right" } }
          ]
        },
        height: 520,
        image_advtab: false,
        image_dimensions: true,
        image_title: true,
        image_uploadtab: true,
        images_file_types: "jpeg,jpg,png,webp,svg",
        images_upload_handler: async (blobInfo) => {
          const response = await uploadAdminFile(blobInfo.blob(), "content", blobInfo.filename());

          return response.url;
        },
        menubar: false,
        object_resizing: "img",
        plugins: "autolink link lists image table code wordcount",
        resize_img_proportional: true,
        setup: (editor) => {
          editor.ui.registry.addButton("twoColumnLayout", {
            text: "2 колонки",
            tooltip: "Вставить блок: текст слева, изображение справа",
            onAction: () => {
              editor.insertContent(
                '<table style="width: 100%; border-collapse: collapse; table-layout: fixed;"><tbody><tr><td style="width: 60%; vertical-align: top; padding: 0.5rem;"><p>Текст слева</p></td><td style="width: 40%; vertical-align: top; padding: 0.5rem;"><p>Вставьте изображение справа</p></td></tr></tbody></table>'
              );
            }
          });
        },
        skin: false,
        toolbar:
          "undo redo | blocks | bold italic underline | bullist numlist | alignleft aligncenter alignright | link unlink | image table twoColumnLayout | removeformat | code",
        toolbar_mode: "sliding"
      }}
      licenseKey="gpl"
      onEditorChange={onChange}
      value={value}
    />
  );
}
